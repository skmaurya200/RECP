using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class DashboardApiController : Controller
    {
        private readonly string cs = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet]
        public JsonResult Summary()
        {
            var recent = new List<object>();
            int departments, content, staff, gallery;
            using (var connection = new SqlConnection(cs))
            {
                connection.Open();
                departments = Count(connection, "SELECT COUNT(1) FROM dbo.tbl_Department");
                content = Count(connection, "SELECT (SELECT COUNT(1) FROM dbo.tbl_GeneralNotice)+(SELECT COUNT(1) FROM dbo.tbl_Event)");
                staff = Count(connection, "SELECT COUNT(1) FROM dbo.tbl_TeachingStaff");
                gallery = Count(connection, "SELECT COUNT(1) FROM dbo.tbl_Gallery");
                using (var command = new SqlCommand("SELECT TOP (5) Title,NoticeType,CreatedAt,IsActive FROM dbo.tbl_GeneralNotice ORDER BY NoticeId DESC", connection))
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) recent.Add(new { Title=reader.GetString(0), Category=reader.GetString(1), Date=reader.GetDateTime(2).ToString("dd MMM yyyy"), IsActive=reader.GetBoolean(3) });
            }

            const long capacity = 10L * 1024 * 1024 * 1024;
            var used = GetFolderSize(Server.MapPath("~/Content/uploads/manager"));
            return Json(new { success=true, counts=new { departments, content, staff, gallery }, recent,
                storage=new { usedBytes=used, usedText=FormatBytes(used), capacityText="10 GB", percent=Math.Min(100, Math.Round(used * 100d / capacity, 1)) }
            }, JsonRequestBehavior.AllowGet);
        }

        private static int Count(SqlConnection connection, string sql) { using (var command=new SqlCommand(sql,connection)) return Convert.ToInt32(command.ExecuteScalar()); }
        private static long GetFolderSize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            try { long total=0; foreach(var file in Directory.EnumerateFiles(path,"*",SearchOption.AllDirectories)) try { total+=new FileInfo(file).Length; } catch (IOException) { } return total; }
            catch (UnauthorizedAccessException) { return 0; }
            catch (IOException) { return 0; }
        }
        private static string FormatBytes(long bytes)
        {
            if(bytes>=1024L*1024*1024)return(bytes/(1024d*1024*1024)).ToString("0.##")+" GB";
            if(bytes>=1024L*1024)return(bytes/(1024d*1024)).ToString("0.##")+" MB";
            if(bytes>=1024)return(bytes/1024d).ToString("0.##")+" KB";
            return bytes+" B";
        }
    }
}
