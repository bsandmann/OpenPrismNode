using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;
using MediatR;
using OpenPrismNode.Core.Commands.IsValidWalletId;

namespace OpenPrismNode.Web.Common
{
    public class ApiKeyOrWalletUserRoleAuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        private const string API_KEY_HEADER_NAME = "Authorization";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1) Check claims first
            var user = context.HttpContext.User;
            bool isWalletUser = user?.IsInRole("WalletUser") ?? false;
            if (isWalletUser)
            {
                // Already authorized by claims, so return
                return;
            }

            // 2) If not claims-authenticated as WalletUser, see if request header might be a valid wallet id
            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues extractedHeader))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Because the filter is synchronous, we block on the mediator call (not best practice, but simple)
            var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
            var validationResult = mediator.Send(new IsValidWalletIdRequest(extractedHeader.ToString())).Result;

            if (validationResult.IsSuccess && validationResult.Value)
            {
                // It's a valid wallet id, so we consider the user authorized
                return;
            }

            // Otherwise, unauthorized
            context.Result = new UnauthorizedResult();
        }
    }
}