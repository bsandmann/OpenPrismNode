using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;

namespace OpenPrismNode.Web.Common;

public class ApiKeyUserAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "Authorization";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var appSettings = context.HttpContext.RequestServices.GetRequiredService<IOptions<AppSettings>>().Value;
        if (!string.IsNullOrWhiteSpace(appSettings.UserAuthorizationKey))
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues extractedApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userApiKey = appSettings.UserAuthorizationKey;
            var adminApiKey = appSettings.AdminAuthorizationKey;

            if (!userApiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
                !adminApiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}