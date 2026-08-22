using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Rec_Partapgarh
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Friendly URLs for the department section, e.g.
            // /department/computer-science-and-engineering/about  ->  Views/Home/CS.cshtml
            routes.MapRoute(
                name: "DepartmentSection",
                url: "department/{dept}/{page}",
                defaults: new { controller = "Home", action = "DepartmentPage", page = "about" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
