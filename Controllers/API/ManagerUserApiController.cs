using Rec_Partapgarh.Models;
using Rec_Partapgarh.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class ManagerUserApiController : Controller
    {
        private readonly string cs = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet]
        public JsonResult List()
        {
            var data = new List<object>();
            using (var connection = new SqlConnection(cs))
            using (var command = new SqlCommand(@"SELECT ManagerUserId,Username,DisplayName,FailedLoginAttempts,LockoutEndUtc,
                                                         IsActive,CreatedAtUtc,LastLoginAtUtc,CreatedBy,UpdatedAtUtc,UpdatedBy
                                                  FROM dbo.tbl_ManagerUser ORDER BY ManagerUserId DESC", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var lockout = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                        data.Add(new {
                            ManagerUserId=reader.GetInt32(0), Username=reader.GetString(1), DisplayName=reader.GetString(2),
                            FailedLoginAttempts=reader.GetInt32(3), LockoutEndUtc=lockout.HasValue?lockout.Value.ToString("yyyy-MM-dd HH:mm:ss"):null,
                            IsTemporarilyLocked=lockout.HasValue&&lockout.Value>DateTime.UtcNow, IsActive=reader.GetBoolean(5),
                            CreatedAtUtc=reader.GetDateTime(6).ToString("yyyy-MM-dd HH:mm:ss"), LastLoginAtUtc=reader.IsDBNull(7)?null:reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss"),
                            CreatedBy=reader.GetString(8), UpdatedAtUtc=reader.IsDBNull(9)?null:reader.GetDateTime(9).ToString("yyyy-MM-dd HH:mm:ss"), UpdatedBy=reader.IsDBNull(10)?null:reader.GetString(10),
                            IsCurrent=reader.GetString(1).Equals(User.Identity.Name,StringComparison.OrdinalIgnoreCase)
                        });
                    }
            }
            return Json(new { success=true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Save(ManagerUserViewModel model)
        {
            model.Username=(model.Username??string.Empty).Trim(); model.DisplayName=(model.DisplayName??string.Empty).Trim();
            var errors=new Dictionary<string,string>();
            if(!Regex.IsMatch(model.Username,@"^[A-Za-z][A-Za-z0-9._-]{3,99}$"))errors["UserUsername"]="Use 4-100 characters; start with a letter and use letters, numbers, dot, underscore or hyphen.";
            if(string.IsNullOrWhiteSpace(model.DisplayName))errors["UserDisplayName"]="Display name is required.";else if(model.DisplayName.Length>150)errors["UserDisplayName"]="Maximum 150 characters allowed.";
            var passwordRequired=model.ManagerUserId==0;
            if(passwordRequired||!string.IsNullOrEmpty(model.Password))
            {
                var passwordError=ValidatePassword(model.Password);
                if(passwordError!=null)errors["UserPassword"]=passwordError;
                if(model.Password!=model.ConfirmPassword)errors["UserConfirmPassword"]="Passwords do not match.";
            }
            if(errors.Count>0)return Json(new{success=false,message="Please correct the validation errors.",errors});
            if(model.ManagerUserId>0)
            {
                var existingUsername=GetUsername(model.ManagerUserId);
                if(existingUsername==null)return Json(new{success=false,message="User not found."});
                if(existingUsername.Equals(User.Identity.Name,StringComparison.OrdinalIgnoreCase))
                {
                    if(!model.IsActive)return Json(new{success=false,message="You cannot block your own account."});
                    if(!model.Username.Equals(existingUsername,StringComparison.OrdinalIgnoreCase))return Json(new{success=false,message="You cannot change your own username while signed in."});
                }
            }
            try
            {
                using(var connection=new SqlConnection(cs))using(var command=connection.CreateCommand())
                {
                    if(model.ManagerUserId==0)
                    {
                        var hashed=ManagerPasswordHasher.Hash(model.Password);
                        command.CommandText=@"INSERT dbo.tbl_ManagerUser(Username,DisplayName,PasswordHash,PasswordSalt,HashIterations,IsActive,CreatedBy)
                                              VALUES(@Username,@DisplayName,@Hash,@Salt,@Iterations,@IsActive,@By)";
                        command.Parameters.Add("@Hash",SqlDbType.NVarChar,200).Value=hashed.Hash;command.Parameters.Add("@Salt",SqlDbType.NVarChar,100).Value=hashed.Salt;command.Parameters.Add("@Iterations",SqlDbType.Int).Value=hashed.Iterations;
                    }
                    else
                    {
                        command.CommandText=string.IsNullOrEmpty(model.Password)
                            ?@"UPDATE dbo.tbl_ManagerUser SET Username=@Username,DisplayName=@DisplayName,IsActive=@IsActive,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@By WHERE ManagerUserId=@Id"
                            :@"UPDATE dbo.tbl_ManagerUser SET Username=@Username,DisplayName=@DisplayName,PasswordHash=@Hash,PasswordSalt=@Salt,HashIterations=@Iterations,FailedLoginAttempts=0,LockoutEndUtc=NULL,IsActive=@IsActive,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@By WHERE ManagerUserId=@Id";
                        if(!string.IsNullOrEmpty(model.Password)){var hashed=ManagerPasswordHasher.Hash(model.Password);command.Parameters.Add("@Hash",SqlDbType.NVarChar,200).Value=hashed.Hash;command.Parameters.Add("@Salt",SqlDbType.NVarChar,100).Value=hashed.Salt;command.Parameters.Add("@Iterations",SqlDbType.Int).Value=hashed.Iterations;}
                        command.Parameters.Add("@Id",SqlDbType.Int).Value=model.ManagerUserId;
                    }
                    command.Parameters.Add("@Username",SqlDbType.NVarChar,100).Value=model.Username;command.Parameters.Add("@DisplayName",SqlDbType.NVarChar,150).Value=model.DisplayName;command.Parameters.Add("@IsActive",SqlDbType.Bit).Value=model.IsActive;command.Parameters.Add("@By",SqlDbType.NVarChar,100).Value=User.Identity.Name;
                    connection.Open();if(command.ExecuteNonQuery()==0)return Json(new{success=false,message="User not found."});
                }
                return Json(new{success=true,message=model.ManagerUserId==0?"User added successfully.":"User updated successfully."});
            }
            catch(SqlException ex)when(ex.Number==2601||ex.Number==2627){return Json(new{success=false,message="Username already exists.",errors=new{UserUsername="Username already exists."}});}
        }

        [HttpPost,ValidateAntiForgeryToken] public JsonResult ToggleBlock(int id){var username=GetUsername(id);if(username==null)return Json(new{success=false,message="User not found."});if(username.Equals(User.Identity.Name,StringComparison.OrdinalIgnoreCase))return Json(new{success=false,message="You cannot block your own account."});using(var c=new SqlConnection(cs))using(var q=new SqlCommand(@"UPDATE dbo.tbl_ManagerUser SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,FailedLoginAttempts=0,LockoutEndUtc=NULL,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@By WHERE ManagerUserId=@Id",c)){q.Parameters.AddWithValue("@Id",id);q.Parameters.AddWithValue("@By",User.Identity.Name);c.Open();q.ExecuteNonQuery();}return Json(new{success=true,message="User block status updated."});}
        [HttpPost,ValidateAntiForgeryToken] public JsonResult Unlock(int id){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("UPDATE dbo.tbl_ManagerUser SET FailedLoginAttempts=0,LockoutEndUtc=NULL,UpdatedAtUtc=SYSUTCDATETIME(),UpdatedBy=@By WHERE ManagerUserId=@Id",c)){q.Parameters.AddWithValue("@Id",id);q.Parameters.AddWithValue("@By",User.Identity.Name);c.Open();if(q.ExecuteNonQuery()==0)return Json(new{success=false,message="User not found."});}return Json(new{success=true,message="Temporary lock removed."});}
        [HttpPost,ValidateAntiForgeryToken] public JsonResult Delete(int id){var username=GetUsername(id);if(username==null)return Json(new{success=false,message="User not found."});if(username.Equals(User.Identity.Name,StringComparison.OrdinalIgnoreCase))return Json(new{success=false,message="You cannot delete your own account."});using(var c=new SqlConnection(cs))using(var q=new SqlCommand("DELETE dbo.tbl_ManagerUser WHERE ManagerUserId=@Id",c)){q.Parameters.AddWithValue("@Id",id);c.Open();q.ExecuteNonQuery();}return Json(new{success=true,message="User deleted successfully."});}
        private string GetUsername(int id){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("SELECT Username FROM dbo.tbl_ManagerUser WHERE ManagerUserId=@Id",c)){q.Parameters.AddWithValue("@Id",id);c.Open();return q.ExecuteScalar()as string;}}
        private static string ValidatePassword(string password){if(string.IsNullOrEmpty(password)||password.Length<12)return"Password must contain at least 12 characters.";if(password.Length>200)return"Password cannot exceed 200 characters.";if(!Regex.IsMatch(password,"[A-Z]"))return"Include at least one uppercase letter.";if(!Regex.IsMatch(password,"[a-z]"))return"Include at least one lowercase letter.";if(!Regex.IsMatch(password,"[0-9]"))return"Include at least one number.";if(!Regex.IsMatch(password,@"[^A-Za-z0-9]"))return"Include at least one special character.";return null;}
    }
}
