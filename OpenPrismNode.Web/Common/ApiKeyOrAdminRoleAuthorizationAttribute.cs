using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;

namespace OpenPrismNode.Web.Common;

public class ApiKeyOrAdminRoleAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "Authorization";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        bool isClaimsAdmin = user?.IsInRole("Admin") ?? false;

        // If user is an Admin via claims, allow
        if (isClaimsAdmin)
        {
            return;
        }

        // Otherwise, check API Key
        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var extractedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var appSettings = context.HttpContext.RequestServices.GetRequiredService<IOptions<AppSettings>>().Value;
        var apiKey = appSettings.AdminAuthorizationKey;

        // If the header key doesn't match, unauthorized
        if (!apiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}