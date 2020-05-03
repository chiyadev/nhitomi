using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Models;

namespace nhitomi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireUserAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public static readonly object UserItemKey = new object();

        public UserPermissions Permissions { get; set; }
        public bool Unrestricted { get; set; }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.Result != null)
                return;

            var cancellationToken = context.HttpContext.RequestAborted;

            var now = DateTime.UtcNow;

            // retrieve user
            var users  = context.HttpContext.RequestServices.GetService<IUserService>();
            var userId = context.HttpContext.Items.TryGetValue(AuthHandler.PayloadItemKey, out var token) ? ((AuthTokenPayload) token).UserId : default;

            var result = await users.GetAsync(userId, cancellationToken);

            if (!(result.Value is DbUser user))
            {
                context.Result = ResultUtilities.Forbidden($"Unknown user {userId}.");
                return;
            }

            // pass user down the pipeline
            context.HttpContext.Items[UserItemKey] = user;

            if (!user.HasPermissions(UserPermissions.Administrator)) // bypass checks for admin
            {
                // restriction check
                var restriction = user.Restrictions?.FirstOrDefault(r => r.EndTime == null || now < r.EndTime);

                if (Unrestricted && restriction != null)
                {
                    context.Result = ResultUtilities.Forbidden($"Cannot perform this action while you are restricted: {restriction.Reason ?? "<unknown reason>"}");
                    return;
                }

                // permission check
                if (!user.HasPermissions(Permissions))
                {
                    context.Result = ResultUtilities.Forbidden($"Insufficient permissions to perform this action. Required: {string.Join(", ", Permissions.ToFlags())}");
                    return;
                }
            }
        }
    }
}