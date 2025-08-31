using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.UserService;

public sealed class UserService : IUserService
{
    private readonly OpenShockContext _db;

    public UserService(OpenShockContext db)
    {
        _db = db;
    }
    
    public async Task<BasicUserInfo?> SearchUserDirect(string username, CancellationToken cancellationToken = default)
    {
        return await _db.Users.Where(x => x.Name == username).Select(x => new BasicUserInfo
        {
            Id = x.Id,
            Name = x.Name,
            Image = x.GetImageUrl()
        }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}