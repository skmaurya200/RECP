using System.Web;
using System.Web.Mvc;
using Rec_Partapgarh.Security;

namespace Rec_Partapgarh
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ManagerAreaAuthorizeAttribute());
        }
    }
}
