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
    public class TimeTableApiController : Controller
    {
        private const string Admin="superAdmin";
        private readonly string cs=ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet] public JsonResult List()
        {
            EnsureSchema();var data=new List<object>();
            using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT TimeTableId,SessionName,SemesterType,BranchNames,FilePath,IsActive,CreatedBy,UpdatedBy FROM dbo.tbl_TimeTable ORDER BY TimeTableId DESC",c))
            {c.Open();using(var r=q.ExecuteReader())while(r.Read()){var p=r.GetString(4);data.Add(new{TimeTableId=r.GetInt32(0),SessionName=r.IsDBNull(1)?"-":r.GetString(1),SemesterType=r.IsDBNull(2)?"-":r.GetString(2),BranchNames=r.IsDBNull(3)?"-":r.GetString(3),FilePath=p,FileUrl=Url.Content(p),IsActive=r.GetBoolean(5),CreatedBy=r.GetString(6),UpdatedBy=r.IsDBNull(7)?null:r.GetString(7)});}}
            return Json(new{success=true,data},JsonRequestBehavior.AllowGet);
        }

        [HttpPost,ValidateAntiForgeryToken] public JsonResult Save(ManagerTimeTable model)
        {
            EnsureSchema();model.SessionName=(model.SessionName??"").Trim();model.SemesterType=(model.SemesterType??"").Trim();model.BranchNames=NormalizeBranches(model.BranchNames);var file=Request.Files["TimeTableFile"];var errors=new Dictionary<string,string>();
            if(string.IsNullOrWhiteSpace(model.SessionName))errors["TimeTableSession"]="Academic Session is required.";else if(model.SessionName.Length>20)errors["TimeTableSession"]="Maximum 20 characters allowed.";
            var semester=NormalizeSemesters(model.SemesterType);if(semester==null)errors["TimeTableSemester"]="Select at least one valid semester.";else model.SemesterType=semester;
            if(string.IsNullOrWhiteSpace(model.BranchNames))errors["TimeTableBranch"]="Enter at least one branch.";else if(model.BranchNames.Length>500)errors["TimeTableBranch"]="Maximum 500 characters allowed.";
            ValidateFile(file,model.TimeTableId==0,errors);if(errors.Count>0)return Json(new{success=false,message="Please correct the validation errors.",errors});
            var old=model.TimeTableId>0?GetFile(model.TimeTableId):null;if(model.TimeTableId>0&&old==null)return Json(new{success=false,message="Time table not found."});string added=null;
            try
            {
                if(file!=null&&file.ContentLength>0)added=SaveFile(file);
                using(var c=new SqlConnection(cs))using(var q=c.CreateCommand())
                {
                    if(model.TimeTableId==0)q.CommandText="INSERT dbo.tbl_TimeTable(SessionName,SemesterType,BranchNames,FilePath,IsActive,CreatedBy) VALUES(@session,@semester,@branches,@file,1,@by)";
                    else{q.CommandText="UPDATE dbo.tbl_TimeTable SET SessionName=@session,SemesterType=@semester,BranchNames=@branches,FilePath=@file,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE TimeTableId=@id";q.Parameters.AddWithValue("@id",model.TimeTableId);}
                    q.Parameters.AddWithValue("@session",model.SessionName);q.Parameters.AddWithValue("@semester",model.SemesterType);q.Parameters.AddWithValue("@branches",model.BranchNames);q.Parameters.AddWithValue("@file",added??old);q.Parameters.AddWithValue("@by",Admin);c.Open();q.ExecuteNonQuery();
                }
                if(added!=null&&old!=null)DeleteFile(old);return Json(new{success=true,message=model.TimeTableId==0?"Time table added successfully.":"Time table updated successfully."});
            }
            catch(Exception ex){if(added!=null)DeleteFile(added);System.Diagnostics.Trace.TraceError("Time table save error: {0}",ex);return Json(new{success=false,message=HttpContext.IsDebuggingEnabled?"Time table save error: "+ex.Message:"Unable to save time table."});}
        }

        [HttpPost,ValidateAntiForgeryToken]public JsonResult ToggleStatus(int id){return Run("UPDATE dbo.tbl_TimeTable SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE TimeTableId=@id",id,"Time table status updated.");}
        [HttpPost,ValidateAntiForgeryToken]public JsonResult Delete(int id){var path=GetFile(id);if(path==null)return Json(new{success=false,message="Time table not found."});var result=Run("DELETE dbo.tbl_TimeTable WHERE TimeTableId=@id",id,"Time table deleted successfully.");DeleteFile(path);return result;}
        private void EnsureSchema(){const string sql=@"IF COL_LENGTH('dbo.tbl_TimeTable','BranchNames') IS NULL ALTER TABLE dbo.tbl_TimeTable ADD BranchNames NVARCHAR(500) NULL; DECLARE @drop nvarchar(max)=''; SELECT @drop=@drop+'ALTER TABLE dbo.tbl_TimeTable DROP CONSTRAINT '+QUOTENAME(name)+';' FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.tbl_TimeTable') AND (definition LIKE '%StudyYear%' OR definition LIKE '%SemesterType%'); IF LEN(@drop)>0 EXEC sp_executesql @drop; ALTER TABLE dbo.tbl_TimeTable ALTER COLUMN StudyYear NVARCHAR(20) NULL; ALTER TABLE dbo.tbl_TimeTable ALTER COLUMN SessionName NVARCHAR(20) NULL; ALTER TABLE dbo.tbl_TimeTable ALTER COLUMN SemesterType NVARCHAR(100) NULL;";using(var c=new SqlConnection(cs))using(var q=new SqlCommand(sql,c)){c.Open();q.ExecuteNonQuery();}}
        private static readonly string[] Semesters={"Ist Semester","IInd Semester","IIIrd Semester","IVth Semester","Vth Semester","VIth Semester","VIIth Semester","VIIIth Semester"};
        private static readonly string[] SemesterRomans={"I","II","III","IV","V","VI","VII","VIII"};
        // Manager panel comma separated semesters bhejta hai; yahan unhe display form mein badal dete hain
        // (ek selected ho to "Ist Semester", multiple ho to "I/III/IV Semester"). Invalid input par null.
        private static string NormalizeSemesters(string value){var picked=new List<int>();foreach(var token in (value??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0)){var index=Array.FindIndex(Semesters,s=>string.Equals(s,token,StringComparison.OrdinalIgnoreCase));if(index<0)return null;if(!picked.Contains(index))picked.Add(index);}if(picked.Count==0)return null;picked.Sort();return picked.Count==1?Semesters[picked[0]]:string.Join("/",picked.Select(i=>SemesterRomans[i]))+" Semester";}
        private static string NormalizeBranches(string value){if(string.IsNullOrWhiteSpace(value))return"";return string.Join(", ",value.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase));}
        private static void ValidateFile(System.Web.HttpPostedFileBase f,bool required,Dictionary<string,string> errors){if(required&&(f==null||f.ContentLength==0))errors["TimeTableFile"]="PDF or Word file is required.";if(f!=null&&f.ContentLength>0){var ext=Path.GetExtension(f.FileName).ToLowerInvariant();if(ext!=".pdf"&&ext!=".doc"&&ext!=".docx")errors["TimeTableFile"]="Only PDF, DOC or DOCX files are allowed.";else if(f.ContentLength>3*1024*1024)errors["TimeTableFile"]="File size cannot exceed 3 MB.";}}
        private string SaveFile(System.Web.HttpPostedFileBase f){var dir=Server.MapPath("~/Content/uploads/manager/timetables");Directory.CreateDirectory(dir);var name=Guid.NewGuid().ToString("N")+Path.GetExtension(f.FileName).ToLowerInvariant();f.SaveAs(Path.Combine(dir,name));return"~/Content/uploads/manager/timetables/"+name;}
        private string GetFile(int id){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT FilePath FROM dbo.tbl_TimeTable WHERE TimeTableId=@id",c)){q.Parameters.AddWithValue("@id",id);c.Open();return q.ExecuteScalar()as string;}}
        private void DeleteFile(string path){if(string.IsNullOrEmpty(path)||!path.StartsWith("~/Content/uploads/manager/timetables/",StringComparison.OrdinalIgnoreCase))return;var physical=Server.MapPath(path);if(System.IO.File.Exists(physical))System.IO.File.Delete(physical);}
        private JsonResult Run(string sql,int id,string message){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(sql,c)){q.Parameters.AddWithValue("@id",id);q.Parameters.AddWithValue("@by",Admin);c.Open();if(q.ExecuteNonQuery()==0)return Json(new{success=false,message="Time table not found."});}return Json(new{success=true,message});}
    }
}
