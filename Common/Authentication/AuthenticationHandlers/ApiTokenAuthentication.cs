using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Utils;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OpenShock.Common.Authentication.AuthenticationHandlers;

public sealed class ApiTokenAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IClientAuthService<User> _authService;
    private readonly IUserReferenceService _userReferenceService;
    private readonly IBatchUpdateService _batchUpdateService;
    private readonly OpenShockContext _db;
    private readonly JsonSerializerOptions _serializerOptions;

    public ApiTokenAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IClientAuthService<User> clientAuth,
        IUserReferenceService userReferenceService,
        OpenShockContext db,
        IOptions<JsonOptions> jsonOptions, IBatchUpdateService batchUpdateService)
        : base(options, logger, encoder)
    {
        _authService = clientAuth;
        _userReferenceService = userReferenceService;
        _db = db;
        _serializerOptions = jsonOptions.Value.SerializerOptions;
        _batchUpdateService = batchUpdateService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.TryGetApiTokenFromHeader(out var token))
        {
            return AuthenticateResult.Fail(AuthResultError.HeaderMissingOrInvalid.Title!);
        }

        string tokenHash = HashingUtils.HashSha256(token);

        var tokenDto = await _db.ApiTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == tokenHash &&
            (x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow));
        if (tokenDto == null) return AuthenticateResult.Fail(AuthResultError.TokenInvalid.Title!);

        _batchUpdateService.UpdateTokenLastUsed(tokenDto.Id);
        _authService.CurrentClient = tokenDto.User;
        _userReferenceService.AuthReference = tokenDto;

        List<Claim> claims = [
            new(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemas.ApiToken),
            new(ClaimTypes.NameIdentifier, tokenDto.User.Id.ToString()),
            new(OpenShockAuthClaims.ApiTokenId, tokenDto.Id.ToString())
        ];

        foreach (var perm in tokenDto.Permissions)
        {
            claims.Add(new(OpenShockAuthClaims.ApiTokenPermission, perm.ToString()));
        }

        var ident = new ClaimsIdentity(claims, nameof(ApiTokenAuthentication));

        Context.User = new ClaimsPrincipal(ident);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(ident), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}