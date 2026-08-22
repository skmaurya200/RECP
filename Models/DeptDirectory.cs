using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;

namespace Rec_Partapgarh.Models
{
    /// <summary>
    /// Maps the static department page codes (CS, ELE, CVLE, MCHE, ASH) to the
    /// DepartmentName stored in dbo.tbl_Department, and protects that name so it
    /// can travel in the query string of the shared faculty page.
    /// </summary>
    public static class DeptDirectory
    {
        private const string Purpose = "DepartmentName";

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CS", "Computer Science & Engineering" },
            { "ELE", "Electrical Engineering" },
            { "CVLE", "Civil Engineering" },
            { "MCHE", "Mechanical Engineering" },
            { "ASH", "Applied Sciences and Humanities" }
        };

        public static string NameOf(string code) { string n; return Names.TryGetValue(code ?? "", out n) ? n : null; }

        /// <summary>Sidebar partial to render for a department name coming back from the token.</summary>
        public static string SidebarFor(string departmentName)
        {
            var code = Names.FirstOrDefault(x => string.Equals(x.Value, departmentName, StringComparison.OrdinalIgnoreCase)).Key;
            return code == null ? null : "~/Views/Shared/DeptSidebar/_" + code + "_Sidebar.cshtml";
        }

        /// <summary>Encrypted department name for the sidebar Faculty link.</summary>
        public static string Token(string code)
        {
            var name = NameOf(code);
            if (name == null) return null;
            return HttpServerUtility.UrlTokenEncode(MachineKey.Protect(Encoding.UTF8.GetBytes(name), Purpose));
        }

        public static bool TryReadToken(string token, out string departmentName)
        {
            departmentName = null;
            if (string.IsNullOrWhiteSpace(token)) return false;
            try
            {
                var encoded = HttpServerUtility.UrlTokenDecode(token);
                if (encoded == null) return false;
                var bytes = MachineKey.Unprotect(encoded, Purpose);
                if (bytes == null) return false;
                departmentName = Encoding.UTF8.GetString(bytes);
                return departmentName.Length > 0;
            }
            catch { return false; }
        }
    }
}
