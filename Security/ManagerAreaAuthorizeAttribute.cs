using Rec_Partapgarh.Controllers;
using System;
using System.Web;
using System.Web.Mvc;

namespace Rec_Partapgarh.Security
{
    public sealed class ManagerAreaAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var controller = filterContext.Controller;
            var controllerType = controller.GetType();
            var isManagerPage = controller is ManagerController;
            var isManagerApi = (controllerType.Namespace ?? string.Empty).StartsWith("Rec_Partapgarh.Controllers.API", StringComparison.Ordinal);
            if (!isManagerPage && !isManagerApi) return;

            // Protected content must never be restored from the browser cache after logout.
            var cache = filterContext.HttpContext.Response.Cache;
            cache.SetCacheability(HttpCacheability.NoCache);
            cache.SetNoStore();
            cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Headers["Pragma"] = "no-cache";
            base.OnAuthorization(filterContext);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var controllerNamespace = filterContext.Controller.GetType().Namespace ?? string.Empty;
            if (controllerNamespace.StartsWith("Rec_Partapgarh.Controllers.API", StringComparison.Ordinal))
            {
                filterContext.HttpContext.Response.StatusCode = 401;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.Result = new JsonResult { Data = new { success = false, message = "Your session has expired. Please sign in again." }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                return;
            }
            base.HandleUnauthorizedRequest(filterContext);
        }
    }
}
