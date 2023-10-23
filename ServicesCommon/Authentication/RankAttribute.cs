using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.Common.Models;

namespace OpenShock.ServicesCommon.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RankAttribute : Attribute, IAuthorizationFilter
{
    private RankType requiredRank;
    
    public RankAttribute(RankType requiredRank)
    {
        this.requiredRank = requiredRank;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.RequestServices.GetService<IClientAuthService<LinkUser>>()?.CurrentClient;
        if (user == null)
        {
            context.Result = new JsonResult(new BaseResponse<object>
            {
                Message = "Error while authorizing request",
            });
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }
        if(user.DbUser.Rank.IsAllowed(requiredRank)) return;
        
        context.Result = new JsonResult(new BaseResponse<object>
        {
            Message = $"Required rank not met. Required rank is {requiredRank} but you only have {user.DbUser.Rank}"
        });
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
    }
}