using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class SyllabusApiController : Controller
    {
        private const string Admin="superAdmin";
        private readonly string cs=ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet] public JsonResult List()
        {
            EnsureSchema();var data=new List<object>();
            using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT s.SyllabusId,s.CourseName,s.StudyYear,COALESCE(NULLIF(s.BranchNames,''),d.DepartmentName),s.FilePath,s.IsActive,s.CreatedBy,s.UpdatedBy FROM dbo.tbl_Syllabus s LEFT JOIN dbo.tbl_Department d ON d.DepartmentId=s.DepartmentId ORDER BY s.SyllabusId DESC",c))
            {c.Open();using(var r=q.ExecuteReader())while(r.Read()){var p=r.GetString(4);data.Add(new{SyllabusId=r.GetInt32(0),CourseName=r.GetString(1),StudyYear=r.GetString(2),BranchNames=r.IsDBNull(3)?"-":r.GetString(3),FilePath=p,FileUrl=Url.Content(p),IsActive=r.GetBoolean(5),CreatedBy=r.GetString(6),UpdatedBy=r.IsDBNull(7)?null:r.GetString(7)});}}
            return Json(new{success=true,data},JsonRequestBehavior.AllowGet);
        }

        [HttpPost,ValidateAntiForgeryToken] public JsonResult Save(ManagerSyllabus m)
        {
            EnsureSchema();m.CourseName=(m.CourseName??"").Trim();m.BranchNames=NormalizeBranches(m.BranchNames);m.StudyYear=(m.StudyYear??"").Trim();var file=Request.Files["SyllabusFile"];var errors=new Dictionary<string,string>();
            if(string.IsNullOrWhiteSpace(m.CourseName))errors["SyllabusCourse"]="Programme is required.";else if(m.CourseName.Length>150)errors["SyllabusCourse"]="Maximum 150 characters allowed.";
            if(string.IsNullOrWhiteSpace(m.BranchNames))errors["SyllabusBranch"]="Enter at least one branch.";else if(m.BranchNames.Length>500)errors["SyllabusBranch"]="Maximum 500 characters allowed.";
            if(!ValidYear(m.StudyYear))errors["SyllabusYear"]="Select a valid year.";ValidateFile(file,m.SyllabusId==0,errors);if(errors.Count>0)return Json(new{success=false,message="Please correct the validation errors.",errors});
            var old=m.SyllabusId>0?GetFile(m.SyllabusId):null;if(m.SyllabusId>0&&old==null)return Json(new{success=false,message="Syllabus not found."});string added=null;
            try
            {
                if(file!=null&&file.ContentLength>0)added=SaveFile(file);
                using(var c=new SqlConnection(cs))using(var q=c.CreateCommand())
                {
                    if(m.SyllabusId==0){q.CommandText="INSERT dbo.tbl_Syllabus(CourseName,StudyYear,BranchNames,FilePath,IsActive,CreatedBy) VALUES(@p,@y,@b,@f,1,@by)";}
                    else{q.CommandText="UPDATE dbo.tbl_Syllabus SET CourseName=@p,StudyYear=@y,BranchNames=@b,FilePath=@f,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE SyllabusId=@id";q.Parameters.AddWithValue("@id",m.SyllabusId);}
                    q.Parameters.AddWithValue("@p",m.CourseName);q.Parameters.AddWithValue("@y",m.StudyYear);q.Parameters.AddWithValue("@b",m.BranchNames);q.Parameters.AddWithValue("@f",added??old);q.Parameters.AddWithValue("@by",Admin);c.Open();q.ExecuteNonQuery();
                }
                if(added!=null&&old!=null)DeleteFile(old);return Json(new{success=true,message=m.SyllabusId==0?"Syllabus added successfully.":"Syllabus updated successfully."});
            }
            catch(SqlException ex){if(added!=null)DeleteFile(added);LogFailure("SQL "+ex.Number+": "+ex.Message,m);var message=HttpContext.IsDebuggingEnabled?"Syllabus database error ("+ex.Number+"): "+ex.Message:"Unable to save syllabus. Please verify the entered data and try again.";return Json(new{success=false,message});}
            catch(Exception ex){if(added!=null)DeleteFile(added);LogFailure(ex.ToString(),m);var message=HttpContext.IsDebuggingEnabled?"Syllabus save error: "+ex.Message:"Unable to save syllabus.";return Json(new{success=false,message});}
        }

        [HttpPost,ValidateAntiForgeryToken]public JsonResult ToggleStatus(int id){return Run("UPDATE dbo.tbl_Syllabus SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE SyllabusId=@id",id,"Syllabus status updated.");}
        [HttpPost,ValidateAntiForgeryToken]public JsonResult Delete(int id){var p=GetFile(id);if(p==null)return Json(new{success=false,message="Syllabus not found."});var r=Run("DELETE dbo.tbl_Syllabus WHERE SyllabusId=@id",id,"Syllabus deleted successfully.");DeleteFile(p);return r;}
        private void EnsureSchema(){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("IF COL_LENGTH('dbo.tbl_Syllabus','BranchNames') IS NULL ALTER TABLE dbo.tbl_Syllabus ADD BranchNames NVARCHAR(500) NULL; IF COL_LENGTH('dbo.tbl_Syllabus','StudyYear') < 40 ALTER TABLE dbo.tbl_Syllabus ALTER COLUMN StudyYear NVARCHAR(20) NOT NULL",c)){c.Open();q.ExecuteNonQuery();}}
        private static string NormalizeBranches(string value){if(string.IsNullOrWhiteSpace(value))return"";return string.Join(", ",value.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase));}
        private static bool ValidYear(string value){return value=="First Year"||value=="Second Year"||value=="Third Year"||value=="Fourth Year";}
        private static void ValidateFile(System.Web.HttpPostedFileBase f,bool required,Dictionary<string,string> e){if(required&&(f==null||f.ContentLength==0))e["SyllabusFile"]="PDF or Word file is required.";if(f!=null&&f.ContentLength>0){var x=Path.GetExtension(f.FileName).ToLowerInvariant();if(x!=".pdf"&&x!=".doc"&&x!=".docx")e["SyllabusFile"]="Only PDF, DOC or DOCX files are allowed.";else if(f.ContentLength>3*1024*1024)e["SyllabusFile"]="File size cannot exceed 3 MB.";}}
        private string SaveFile(System.Web.HttpPostedFileBase f){var dir=Server.MapPath("~/Content/uploads/manager/syllabus");Directory.CreateDirectory(dir);var name=Guid.NewGuid().ToString("N")+Path.GetExtension(f.FileName).ToLowerInvariant();f.SaveAs(Path.Combine(dir,name));return"~/Content/uploads/manager/syllabus/"+name;}
        private string GetFile(int id){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT FilePath FROM dbo.tbl_Syllabus WHERE SyllabusId=@id",c)){q.Parameters.AddWithValue("@id",id);c.Open();return q.ExecuteScalar()as string;}}
        private void DeleteFile(string p){if(string.IsNullOrEmpty(p)||!p.StartsWith("~/Content/uploads/manager/syllabus/",StringComparison.OrdinalIgnoreCase))return;var x=Server.MapPath(p);if(System.IO.File.Exists(x))System.IO.File.Delete(x);}
        private void LogFailure(string error,ManagerSyllabus model){try{var line=DateTime.UtcNow.ToString("O")+" | Id="+model.SyllabusId+" | ProgrammeLength="+(model.CourseName??"").Length+" | BranchLength="+(model.BranchNames??"").Length+" | Year="+model.StudyYear+" | "+error+Environment.NewLine;System.IO.File.AppendAllText(Server.MapPath("~/App_Data/manager-syllabus-errors.log"),line);}catch{/* Logging must never hide the original failure. */}}
        private JsonResult Run(string sql,int id,string message){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(sql,c)){q.Parameters.AddWithValue("@id",id);q.Parameters.AddWithValue("@by",Admin);c.Open();if(q.ExecuteNonQuery()==0)return Json(new{success=false,message="Syllabus not found."});}return Json(new{success=true,message});}
    }
}
