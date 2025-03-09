using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Pinpoint_Quiz.Helpers
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;
        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Retrieve the user role from session
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // If there is no role in session or the user's role isn't in the allowed list...
            if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
            {
                // Redirect to a NoAccess page (you can create a NoAccess action in HomeController)
                context.Result = new RedirectToActionResult("NoAccess", "Home", null);
                return;
            }
        }
    }
}
