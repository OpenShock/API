using OpenShock.Common.Models;

namespace OpenShock.API.Services.UserService;

public interface IUserService
{
    public Task<BasicUserInfo?> SearchUserDirect(string username, CancellationToken cancellationToken = default);
}