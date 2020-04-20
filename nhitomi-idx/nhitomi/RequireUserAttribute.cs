using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Models;

namespace nhitomi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireUserAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public static readonly object UserItemKey = new object();

        public UserPermissions Permissions { get; set; }
        public bool Unrestricted { get; set; }
        public int Reputation { get; set; }

        /// <summary>
        /// If not null, the authenticated user must have all specified <see cref="Permissions"/> OR their ID must match route parameter specified by this property.
        /// </summary>
        public string AllowSelf { get; set; }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.Result != null)
                return;

            var cancellationToken = context.HttpContext.RequestAborted;

            var now = DateTime.UtcNow;

            // retrieve user
            var users  = context.HttpContext.RequestServices.GetService<IUserService>();
            var userId = context.HttpContext.Items.TryGetValue(AuthHandler.PayloadItemKey, out var token) ? ((AuthTokenPayload) token).UserId : default;

            var user = await users.GetAsync(userId, cancellationToken);

            if (user == null)
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
                bool isSelf() => AllowSelf != null && context.RouteData.Values.TryGetValue(AllowSelf, out var v) && v?.ToString() == userId;

                if (!user.HasPermissions(Permissions) && !isSelf())
                {
                    context.Result = ResultUtilities.Forbidden($"Insufficient permissions to perform this action. Required: {string.Join(", ", Permissions.ToFlags())}");
                    return;
                }

                // reputation check
                //todo:
            }
        }
    }
}