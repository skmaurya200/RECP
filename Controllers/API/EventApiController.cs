using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class EventApiController : Controller
    {
        private const string Admin = "superAdmin";
        private readonly string cs = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] ImageTypes = { "image/jpeg", "image/png", "image/webp" };

        [HttpGet]
        public JsonResult List()
        {
            var data = new List<object>();
            using (var c = new SqlConnection(cs))
            using (var q = new SqlCommand("SELECT EventId,EventName,Venue,EventDate,EventTime,IsActive,CreatedBy,UpdatedBy,BannerImagePath FROM dbo.tbl_Event ORDER BY EventId DESC", c))
            {
                c.Open();
                using (var r = q.ExecuteReader())
                    while (r.Read())
                    {
                        var banner = r.IsDBNull(8) ? null : r.GetString(8);
                        data.Add(new { EventId=r.GetInt32(0), EventName=r.GetString(1), Venue=r.GetString(2), EventDate=r.GetDateTime(3).ToString("yyyy-MM-dd"), EventTime=r.GetTimeSpan(4).ToString(@"hh\:mm"), IsActive=r.GetBoolean(5), CreatedBy=r.GetString(6), UpdatedBy=r.IsDBNull(7)?null:r.GetString(7), BannerImagePath=banner, BannerImageUrl=banner==null?null:Url.Content(banner) });
                    }
            }
            return Json(new { success=true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Save(int EventId, string EventName, string Venue, string EventDate, string EventTime, bool IsActive)
        {
            EventName=(EventName??"").Trim(); Venue=(Venue??"").Trim();
            var image=Request.Files["BannerImage"];
            var errors=new Dictionary<string,string>(); DateTime date; TimeSpan time;
            if(string.IsNullOrWhiteSpace(EventName))errors["EventName"]="Event name is required."; else if(EventName.Length>200)errors["EventName"]="Maximum 200 characters allowed.";
            if(string.IsNullOrWhiteSpace(Venue))errors["Venue"]="Venue is required."; else if(Venue.Length>200)errors["Venue"]="Maximum 200 characters allowed.";
            if(!DateTime.TryParseExact(EventDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date))errors["EventDate"]="Valid event date is required.";
            if(!TimeSpan.TryParseExact(EventTime,@"hh\:mm",CultureInfo.InvariantCulture,out time))errors["EventTime"]="Valid event time is required.";
            if(EventId==0&&(image==null||image.ContentLength==0))errors["BannerImage"]="Event banner image is required.";
            if(image!=null&&image.ContentLength>0){var ext=Path.GetExtension(image.FileName).ToLowerInvariant();var type=(image.ContentType??"").ToLowerInvariant();if(!ImageExtensions.Contains(ext)||!ImageTypes.Contains(type))errors["BannerImage"]="Only JPG, PNG or WEBP images are allowed.";else if(image.ContentLength>3*1024*1024)errors["BannerImage"]="Image size cannot exceed 3 MB.";}
            if(errors.Count>0)return Json(new { success=false, message="Please correct the validation errors.", errors });

            var old=EventId>0?GetBanner(EventId):null; string added=null;
            try
            {
                if(image!=null&&image.ContentLength>0){var dir=Server.MapPath("~/Content/uploads/manager/events");Directory.CreateDirectory(dir);var name=Guid.NewGuid().ToString("N")+Path.GetExtension(image.FileName).ToLowerInvariant();image.SaveAs(Path.Combine(dir,name));added="~/Content/uploads/manager/events/"+name;}
                using(var c=new SqlConnection(cs))using(var q=c.CreateCommand())
                {
                    q.CommandText=EventId==0?"INSERT dbo.tbl_Event(EventName,Venue,EventDate,EventTime,BannerImagePath,IsActive,CreatedBy) VALUES(@n,@v,@d,@t,@b,@s,@by)":"UPDATE dbo.tbl_Event SET EventName=@n,Venue=@v,EventDate=@d,EventTime=@t,BannerImagePath=@b,IsActive=@s,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE EventId=@id";
                    q.Parameters.AddWithValue("@n",EventName);q.Parameters.AddWithValue("@v",Venue);q.Parameters.AddWithValue("@d",date);q.Parameters.AddWithValue("@t",time);q.Parameters.AddWithValue("@b",(object)(added??old)??DBNull.Value);q.Parameters.AddWithValue("@s",IsActive);q.Parameters.AddWithValue("@by",Admin);if(EventId>0)q.Parameters.AddWithValue("@id",EventId);c.Open();if(q.ExecuteNonQuery()==0)return Json(new{success=false,message="Event not found."});
                }
                if(added!=null&&old!=null)DeleteBanner(old);
                return Json(new{success=true,message=EventId==0?"Event added successfully.":"Event updated successfully."});
            }
            catch{if(added!=null)DeleteBanner(added);return Json(new{success=false,message="Unable to save event."});}
        }

        [HttpPost,ValidateAntiForgeryToken] public JsonResult ToggleStatus(int id){return Run("UPDATE dbo.tbl_Event SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE EventId=@id",id,"Event status updated.");}
        [HttpPost,ValidateAntiForgeryToken] public JsonResult Delete(int id){var banner=GetBanner(id);var result=Run("DELETE dbo.tbl_Event WHERE EventId=@id",id,"Event deleted successfully.");if(banner!=null)DeleteBanner(banner);return result;}
        private JsonResult Run(string sql,int id,string message){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(sql,c)){q.Parameters.AddWithValue("@id",id);if(sql.Contains("@by"))q.Parameters.AddWithValue("@by",Admin);c.Open();if(q.ExecuteNonQuery()==0)return Json(new{success=false,message="Event not found."});}return Json(new{success=true,message});}
        private string GetBanner(int id){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT BannerImagePath FROM dbo.tbl_Event WHERE EventId=@id",c)){q.Parameters.AddWithValue("@id",id);c.Open();var value=q.ExecuteScalar();return value==null||value==DBNull.Value?null:(string)value;}}
        private void DeleteBanner(string path){if(string.IsNullOrEmpty(path)||!path.StartsWith("~/Content/uploads/manager/events/",StringComparison.OrdinalIgnoreCase))return;var physical=Server.MapPath(path);if(System.IO.File.Exists(physical))System.IO.File.Delete(physical);}
    }
}
