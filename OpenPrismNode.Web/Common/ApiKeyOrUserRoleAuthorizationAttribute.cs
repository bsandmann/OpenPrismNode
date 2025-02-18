using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;

namespace OpenPrismNode.Web.Common;

public class ApiKeyOrUserRoleAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "Authorization";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 1. Check if the user is logged in and has either User or Admin role
        var user = context.HttpContext.User;
        bool isUser = user?.IsInRole("User") ?? false;
        bool isAdmin = user?.IsInRole("Admin") ?? false;

        if (isUser || isAdmin)
        {
            // Already authorized via claims-based auth
            return;
        }

        // 2. If not claims-authenticated, fallback to API key check
        var appSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<AppSettings>>().Value;

        // If there's no user key configured, there's no key-based check to do,
        // but we also want to allow the Admin's key to pass.

        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues extractedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userApiKey = appSettings.UserAuthorizationKey;
        var adminApiKey = appSettings.AdminAuthorizationKey;

        // Compare extracted key against both user and admin keys
        if (!userApiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase)
            && !adminApiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
