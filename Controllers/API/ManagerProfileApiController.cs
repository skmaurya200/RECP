using Rec_Partapgarh.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class ManagerProfileApiController : Controller
    {
        private readonly string cs=ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;
        private static readonly HashSet<string> Extensions=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".jpg",".jpeg",".png",".webp"};
        private static readonly HashSet<string> Types=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"image/jpeg","image/png","image/webp"};

        [HttpGet]
        public JsonResult Current()
        {
            using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT Username,DisplayName,ProfileImagePath FROM dbo.tbl_ManagerUser WHERE Username=@u AND IsActive=1",c))
            { q.Parameters.Add("@u",SqlDbType.NVarChar,100).Value=User.Identity.Name;c.Open();using(var r=q.ExecuteReader())if(r.Read()){var path=r.IsDBNull(2)?null:r.GetString(2);return Json(new{success=true,data=new{Username=r.GetString(0),DisplayName=r.GetString(1),ImageUrl=string.IsNullOrWhiteSpace(path)?Url.Content("~/Content/img/logoREC2.png"):Url.Content(path)}},JsonRequestBehavior.AllowGet);} }
            return Json(new{success=false,message="Signed-in user was not found."},JsonRequestBehavior.AllowGet);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public JsonResult UploadPhoto()
        {
            var file=Request.Files["ProfilePhoto"];
            if(file==null||file.ContentLength==0)return Json(new{success=false,message="Please select a profile photo.",errors=new{ProfilePhoto="Please select a profile photo."}});
            var ext=Path.GetExtension(file.FileName);
            if(!Extensions.Contains(ext)||!Types.Contains(file.ContentType??""))return Json(new{success=false,message="Only JPG, PNG or WEBP images are allowed."});
            if(file.ContentLength>3*1024*1024)return Json(new{success=false,message="Profile photo cannot exceed 3 MB."});
            var folder=Server.MapPath("~/Content/uploads/manager/profile");Directory.CreateDirectory(folder);
            var relative="~/Content/uploads/manager/profile/"+Guid.NewGuid().ToString("N")+ext.ToLowerInvariant();var physical=Server.MapPath(relative);file.SaveAs(physical);
            string old=null;
            try{using(var c=new SqlConnection(cs))using(var q=new SqlCommand(@"UPDATE dbo.tbl_ManagerUser SET ProfileImagePath=@p,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@u OUTPUT deleted.ProfileImagePath WHERE Username=@u AND IsActive=1",c)){q.Parameters.Add("@p",SqlDbType.NVarChar,300).Value=relative;q.Parameters.Add("@u",SqlDbType.NVarChar,100).Value=User.Identity.Name;c.Open();old=q.ExecuteScalar()as string;if(old==null&&!Exists(User.Identity.Name)){System.IO.File.Delete(physical);return Json(new{success=false,message="Signed-in user was not found."});}}DeleteOld(old);return Json(new{success=true,message="Profile photo updated successfully.",imageUrl=Url.Content(relative)});}
            catch{if(System.IO.File.Exists(physical))System.IO.File.Delete(physical);return Json(new{success=false,message="Unable to update profile photo."});}
        }

        [HttpPost,ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string CurrentPassword,string NewPassword,string ConfirmPassword)
        {
            var errors=new Dictionary<string,string>();if(string.IsNullOrEmpty(CurrentPassword))errors["CurrentPassword"]="Current password is required.";var passwordError=ValidatePassword(NewPassword);if(passwordError!=null)errors["NewPassword"]=passwordError;if(NewPassword!=ConfirmPassword)errors["ConfirmPassword"]="Passwords do not match.";if(errors.Count>0)return Json(new{success=false,message="Please correct the validation errors.",errors});
            using(var c=new SqlConnection(cs)){c.Open();using(var tx=c.BeginTransaction(IsolationLevel.Serializable)){string hash,salt;int iterations;using(var q=new SqlCommand("SELECT PasswordHash,PasswordSalt,HashIterations FROM dbo.tbl_ManagerUser WITH(UPDLOCK,ROWLOCK) WHERE Username=@u AND IsActive=1",c,tx)){q.Parameters.Add("@u",SqlDbType.NVarChar,100).Value=User.Identity.Name;using(var r=q.ExecuteReader()){if(!r.Read())return Json(new{success=false,message="Signed-in user was not found."});hash=r.GetString(0);salt=r.GetString(1);iterations=r.GetInt32(2);}}if(!ManagerPasswordHasher.Verify(CurrentPassword,hash,salt,iterations))return Json(new{success=false,message="Current password is incorrect.",errors=new{CurrentPassword="Current password is incorrect."}});if(ManagerPasswordHasher.Verify(NewPassword,hash,salt,iterations))return Json(new{success=false,message="Choose a password different from your current password.",errors=new{NewPassword="New password must be different."}});var h=ManagerPasswordHasher.Hash(NewPassword);using(var q=new SqlCommand("UPDATE dbo.tbl_ManagerUser SET PasswordHash=@h,PasswordSalt=@s,HashIterations=@i,FailedLoginAttempts=0,LockoutEndUtc=NULL,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@u WHERE Username=@u",c,tx)){q.Parameters.Add("@h",SqlDbType.NVarChar,200).Value=h.Hash;q.Parameters.Add("@s",SqlDbType.NVarChar,100).Value=h.Salt;q.Parameters.Add("@i",SqlDbType.Int).Value=h.Iterations;q.Parameters.Add("@u",SqlDbType.NVarChar,100).Value=User.Identity.Name;q.ExecuteNonQuery();}tx.Commit();}}
            return Json(new{success=true,message="Password changed successfully. Please sign in again.",requiresLogout=true});
        }
        private bool Exists(string username){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT COUNT(1) FROM dbo.tbl_ManagerUser WHERE Username=@u",c)){q.Parameters.AddWithValue("@u",username);c.Open();return(int)q.ExecuteScalar()>0;}}
        private void DeleteOld(string path){if(string.IsNullOrWhiteSpace(path)||!path.StartsWith("~/Content/uploads/manager/profile/",StringComparison.OrdinalIgnoreCase))return;var file=Server.MapPath(path);if(System.IO.File.Exists(file))System.IO.File.Delete(file);}
        private static string ValidatePassword(string p){if(string.IsNullOrEmpty(p)||p.Length<12)return"Password must contain at least 12 characters.";if(p.Length>200)return"Password cannot exceed 200 characters.";if(!Regex.IsMatch(p,"[A-Z]"))return"Include at least one uppercase letter.";if(!Regex.IsMatch(p,"[a-z]"))return"Include at least one lowercase letter.";if(!Regex.IsMatch(p,"[0-9]"))return"Include at least one number.";if(!Regex.IsMatch(p,@"[^A-Za-z0-9]"))return"Include at least one special character.";return null;}
    }
}
