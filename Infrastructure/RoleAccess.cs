using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Constants;

namespace TechNova_IT_Solutions.Infrastructure
{
    public static class RoleAccess
    {
        public static bool HasAnyRole(HttpContext httpContext, params string[] allowedRoles)
        {
            var userRole = httpContext.Session.GetString(SessionKeys.UserRole);
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return false;
            }

            return allowedRoles.Any(role => string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase));
        }

        public static IActionResult? RequireRoleOrRedirect(
            PageModel page,
            string[] allowedRoles,
            IReadOnlyDictionary<string, string>? redirectByRole = null,
            string fallbackPage = "/Account/Login")
        {
            var userId = page.HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return page.RedirectToPage("/Account/Login");
            }

            if (HasAnyRole(page.HttpContext, allowedRoles))
            {
                return null;
            }

            var role = page.HttpContext.Session.GetString(SessionKeys.UserRole) ?? string.Empty;
            if (redirectByRole != null && redirectByRole.TryGetValue(role, out var redirectPage))
            {
                return page.RedirectToPage(redirectPage);
            }

            return page.RedirectToPage(fallbackPage);
        }

        public static IActionResult? RequireRoleOrUnauthorized(Controller controller, params string[] allowedRoles)
        {
            if (HasAnyRole(controller.HttpContext, allowedRoles))
            {
                return null;
            }

            return controller.Unauthorized(new { success = false, message = "Access denied" });
        }

        public static IActionResult? RequireRoleOrAccessDenied(Controller controller, params string[] allowedRoles)
        {
            var userId = controller.HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return controller.RedirectToAction("Login", "Account");
            }

            if (HasAnyRole(controller.HttpContext, allowedRoles))
            {
                return null;
            }

            return controller.RedirectToAction("AccessDenied", "Account");
        }
    }
}
