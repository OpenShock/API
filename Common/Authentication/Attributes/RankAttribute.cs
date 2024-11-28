using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Models;

namespace OpenShock.Common.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RankAttribute : Attribute, IAuthorizationFilter
{
    private readonly RankType _requiredRank;
    
    public RankAttribute(RankType requiredRank)
    {
        _requiredRank = requiredRank;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.RequestServices.GetService<IClientAuthService<AuthenticatedUser>>()?.CurrentClient;
        if (user == null)
        {
            context.Result = new JsonResult(new BaseResponse<object>
            {
                Message = "Error while authorizing request",
            });
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }
        if(user.DbUser.Rank.IsAllowed(_requiredRank)) return;
        
        context.Result = new JsonResult(new BaseResponse<object>
        {
            Message = $"Required rank not met. Required rank is {_requiredRank} but you only have {user.DbUser.Rank}"
        });
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
    }
}