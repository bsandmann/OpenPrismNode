using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;

namespace OpenPrismNode.Web.Common;

public class ApiKeyAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "Authorization";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues extractedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var appSettings = context.HttpContext.RequestServices.GetRequiredService<IOptions<AppSettings>>().Value;
        var apiKey = appSettings.AuthorizationKey;

        if (!apiKey.Equals(extractedApiKey.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}