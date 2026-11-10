
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace _25DH110266_MyStore.Areas.Admin.Filters
{
    public class AdminSessionAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.Session == null)
                return false;

            string username =
                Convert.ToString(httpContext.Session["Username"]);

            string role =
                Convert.ToString(httpContext.Session["UserRole"]);

            return !string.IsNullOrWhiteSpace(username)
                && string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase);
        }

        protected override void HandleUnauthorizedRequest(
            AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        area = "Admin",
                        controller = "Account",
                        action = "Login"
                    }));
        }
    }
}
