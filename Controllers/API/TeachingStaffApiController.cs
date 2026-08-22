using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class TeachingStaffApiController : Controller
    {
        private const string Admin = "superAdmin";
        private readonly string cs = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;
        private static readonly string[] Ext = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] Types = { "image/jpeg", "image/png", "image/webp" };

        [HttpGet] public JsonResult Branches() { var d = new List<object>(); using (var c = new SqlConnection(cs)) using (var q = new SqlCommand("SELECT DepartmentId,DepartmentName FROM dbo.tbl_Department WHERE IsActive=1 ORDER BY DepartmentName", c)) { c.Open(); using (var r = q.ExecuteReader()) while (r.Read()) d.Add(new { DepartmentId = r.GetInt32(0), DepartmentName = r.GetString(1) }); } return Json(new { success = true, data = d }, JsonRequestBehavior.AllowGet); }

        [HttpGet]
        public JsonResult List()
        {
            EnsureStaffSchema(); var data = new List<object>();
            using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(@"SELECT s.StaffId,s.PhotoPath,s.FullName,s.Email,s.AlternateEmail,s.MobileNumber,s.LandlineNumber,s.Designation,s.DepartmentId,d.DepartmentName,s.Qualification,s.LongDescription,s.DisplayOrder,s.IsActive,s.CreatedBy,s.UpdatedBy
                FROM dbo.tbl_TeachingStaff s JOIN dbo.tbl_Department d ON d.DepartmentId=s.DepartmentId
                ORDER BY CASE WHEN s.DisplayOrder IS NULL THEN 1 ELSE 0 END,s.DisplayOrder,s.StaffId DESC", c))
            { c.Open(); using (var r = q.ExecuteReader()) while (r.Read()) { var p = r.GetString(1); data.Add(new { StaffId = r.GetInt32(0), PhotoPath = p, PhotoUrl = Url.Content(p), FullName = r.GetString(2), Email = r.GetString(3), AlternateEmail = r.IsDBNull(4) ? null : r.GetString(4), MobileNumber = r.IsDBNull(5) ? null : r.GetString(5), LandlineNumber = r.IsDBNull(6) ? null : r.GetString(6), Designation = r.GetString(7), DepartmentId = r.GetInt32(8), DepartmentName = r.GetString(9), Qualification = r.GetString(10), LongDescription = r.IsDBNull(11) ? null : r.GetString(11), DisplayOrder = r.IsDBNull(12) ? (int?)null : r.GetInt32(12), IsActive = r.GetBoolean(13), CreatedBy = r.GetString(14), UpdatedBy = r.IsDBNull(15) ? null : r.GetString(15) }); } }
            return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Save(ManagerTeachingStaff m)
        {
            EnsureStaffSchema(); m.FullName = (m.FullName ?? "").Trim(); m.Email = (m.Email ?? "").Trim().ToLowerInvariant(); m.AlternateEmail = (m.AlternateEmail ?? "").Trim().ToLowerInvariant(); m.MobileNumber = (m.MobileNumber ?? "").Trim(); m.LandlineNumber = (m.LandlineNumber ?? "").Trim(); m.Designation = (m.Designation ?? "").Trim(); m.Qualification = (m.Qualification ?? "").Trim(); m.LongDescription = (m.LongDescription ?? "").Trim(); var f = Request.Files["StaffPhoto"]; var e = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(m.FullName)) e["StaffName"] = "Name is required."; else if (m.FullName.Length > 150) e["StaffName"] = "Maximum 150 characters allowed.";
            try { var a = new MailAddress(m.Email); if (a.Address != m.Email) e["StaffEmail"] = "Enter a valid email."; } catch { e["StaffEmail"] = "Enter a valid email."; }
            if (m.Email.Length > 254) e["StaffEmail"] = "Maximum 254 characters allowed.";
            if (!string.IsNullOrEmpty(m.AlternateEmail)) { try { var a = new MailAddress(m.AlternateEmail); if (a.Address != m.AlternateEmail) e["StaffAlternateEmail"] = "Enter a valid alternate email."; } catch { e["StaffAlternateEmail"] = "Enter a valid alternate email."; } }
            if (!string.IsNullOrEmpty(m.MobileNumber) && !Regex.IsMatch(m.MobileNumber, @"^\+?[0-9][0-9\s-]{7,18}$")) e["StaffMobileNumber"] = "Enter a valid mobile number.";
            if (!string.IsNullOrEmpty(m.LandlineNumber) && !Regex.IsMatch(m.LandlineNumber, @"^\+?[0-9][0-9\s()-]{5,23}$")) e["StaffLandlineNumber"] = "Enter a valid landline number.";
            if (string.IsNullOrWhiteSpace(m.Designation)) e["StaffDesignation"] = "Designation is required."; else if (m.Designation.Length > 150) e["StaffDesignation"] = "Maximum 150 characters allowed."; if (!ValidDepartment(m.DepartmentId)) e["StaffDepartment"] = "Select an active department.";
            if (string.IsNullOrWhiteSpace(m.Qualification)) e["StaffQualification"] = "Qualification is required."; else if (m.Qualification.Length > 250) e["StaffQualification"] = "Maximum 250 characters allowed."; if (m.DisplayOrder.HasValue && (m.DisplayOrder < 1 || m.DisplayOrder > 9999)) e["StaffDisplayOrder"] = "Order must be between 1 and 9999.";
            if (m.StaffId == 0 && (f == null || f.ContentLength == 0)) e["StaffPhoto"] = "Photo is required."; if (f != null && f.ContentLength > 0) { var x = Path.GetExtension(f.FileName).ToLowerInvariant(); var t = (f.ContentType ?? "").ToLowerInvariant(); if (!Ext.Contains(x) || !Types.Contains(t)) e["StaffPhoto"] = "Only JPG, PNG or WEBP images are allowed."; else if (f.ContentLength > 3 * 1024 * 1024) e["StaffPhoto"] = "Photo size cannot exceed 3 MB."; }
            if (e.Count > 0) return Json(new { success = false, message = "Please correct the validation errors.", errors = e });
            var old = m.StaffId > 0 ? GetPhoto(m.StaffId) : null; if (m.StaffId > 0 && old == null) return Json(new { success = false, message = "Staff record not found." }); string added = null;
            try
            {
                if (f != null && f.ContentLength > 0) { var dir = Server.MapPath("~/Content/uploads/manager/staff"); Directory.CreateDirectory(dir); var n = Guid.NewGuid().ToString("N") + Path.GetExtension(f.FileName).ToLowerInvariant(); f.SaveAs(Path.Combine(dir, n)); added = "~/Content/uploads/manager/staff/" + n; }
                using (var c = new SqlConnection(cs)) using (var q = c.CreateCommand()) { q.CommandText = m.StaffId == 0 ? "INSERT dbo.tbl_TeachingStaff(PhotoPath,FullName,Email,AlternateEmail,MobileNumber,LandlineNumber,Designation,DepartmentId,Qualification,LongDescription,DisplayOrder,IsActive,CreatedBy) VALUES(@p,@n,@e,@ae,@m,@ln,@de,@d,@q,@l,@o,@s,@by)" : "UPDATE dbo.tbl_TeachingStaff SET PhotoPath=@p,FullName=@n,Email=@e,AlternateEmail=@ae,MobileNumber=@m,LandlineNumber=@ln,Designation=@de,DepartmentId=@d,Qualification=@q,LongDescription=@l,DisplayOrder=@o,IsActive=@s,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE StaffId=@id"; q.Parameters.AddWithValue("@p", added ?? old); q.Parameters.AddWithValue("@n", m.FullName); q.Parameters.AddWithValue("@e", m.Email); q.Parameters.AddWithValue("@ae", string.IsNullOrEmpty(m.AlternateEmail) ? (object)DBNull.Value : m.AlternateEmail); q.Parameters.AddWithValue("@m", string.IsNullOrEmpty(m.MobileNumber) ? (object)DBNull.Value : m.MobileNumber); q.Parameters.AddWithValue("@ln", string.IsNullOrEmpty(m.LandlineNumber) ? (object)DBNull.Value : m.LandlineNumber); q.Parameters.AddWithValue("@de", m.Designation); q.Parameters.AddWithValue("@d", m.DepartmentId); q.Parameters.AddWithValue("@q", m.Qualification); q.Parameters.Add("@l", SqlDbType.NVarChar, -1).Value = string.IsNullOrEmpty(m.LongDescription) ? (object)DBNull.Value : m.LongDescription; q.Parameters.Add("@o", SqlDbType.Int).Value = m.DisplayOrder.HasValue ? (object)m.DisplayOrder.Value : DBNull.Value; q.Parameters.AddWithValue("@s", m.IsActive); q.Parameters.AddWithValue("@by", Admin); if (m.StaffId > 0) q.Parameters.AddWithValue("@id", m.StaffId); c.Open(); q.ExecuteNonQuery(); }
                if (added != null && old != null) DeletePhoto(old); return Json(new { success = true, message = m.StaffId == 0 ? "Teaching staff added successfully." : "Teaching staff updated successfully." });
            }
            catch (SqlException x) when (x.Number == 2601 || x.Number == 2627) { if (added != null) DeletePhoto(added); return Json(new { success = false, message = "Email already exists.", errors = new { StaffEmail = "Email already exists." } }); }
            catch { if (added != null) DeletePhoto(added); return Json(new { success = false, message = "Unable to save teaching staff." }); }
        }

        [HttpPost, ValidateAntiForgeryToken] public JsonResult ToggleStatus(int id) { return Run("UPDATE dbo.tbl_TeachingStaff SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,UpdatedAt=SYSDATETIME(),UpdatedBy=@by WHERE StaffId=@id", id, "Staff status updated."); }
        [HttpPost, ValidateAntiForgeryToken] public JsonResult Delete(int id) { var p = GetPhoto(id); if (p == null) return Json(new { success = false, message = "Staff record not found." }); var r = Run("DELETE dbo.tbl_TeachingStaff WHERE StaffId=@id", id, "Staff deleted successfully."); DeletePhoto(p); return r; }
        private void EnsureStaffSchema() { using (var c = new SqlConnection(cs)) using (var q = new SqlCommand("IF COL_LENGTH('dbo.tbl_TeachingStaff','DisplayOrder') IS NULL ALTER TABLE dbo.tbl_TeachingStaff ADD DisplayOrder INT NULL;IF COL_LENGTH('dbo.tbl_TeachingStaff','AlternateEmail') IS NULL ALTER TABLE dbo.tbl_TeachingStaff ADD AlternateEmail NVARCHAR(254) NULL;IF COL_LENGTH('dbo.tbl_TeachingStaff','MobileNumber') IS NULL ALTER TABLE dbo.tbl_TeachingStaff ADD MobileNumber NVARCHAR(20) NULL;IF COL_LENGTH('dbo.tbl_TeachingStaff','LandlineNumber') IS NULL ALTER TABLE dbo.tbl_TeachingStaff ADD LandlineNumber NVARCHAR(25) NULL;IF COL_LENGTH('dbo.tbl_TeachingStaff','LongDescription') IS NULL ALTER TABLE dbo.tbl_TeachingStaff ADD LongDescription NVARCHAR(MAX) NULL ELSE IF COL_LENGTH('dbo.tbl_TeachingStaff','LongDescription')<>-1 ALTER TABLE dbo.tbl_TeachingStaff ALTER COLUMN LongDescription NVARCHAR(MAX) NULL", c)) { c.Open(); q.ExecuteNonQuery(); } }
        private bool ValidDepartment(int id) { using (var c = new SqlConnection(cs)) using (var q = new SqlCommand("SELECT COUNT(1) FROM dbo.tbl_Department WHERE DepartmentId=@id AND IsActive=1", c)) { q.Parameters.AddWithValue("@id", id); c.Open(); return (int)q.ExecuteScalar() > 0; } }
        private string GetPhoto(int id) { using (var c = new SqlConnection(cs)) using (var q = new SqlCommand("SELECT PhotoPath FROM dbo.tbl_TeachingStaff WHERE StaffId=@id", c)) { q.Parameters.AddWithValue("@id", id); c.Open(); return q.ExecuteScalar() as string; } }
        private void DeletePhoto(string p) { if (string.IsNullOrEmpty(p) || !p.StartsWith("~/Content/uploads/manager/staff/", StringComparison.OrdinalIgnoreCase)) return; var x = Server.MapPath(p); if (System.IO.File.Exists(x)) System.IO.File.Delete(x); }
        private JsonResult Run(string sql, int id, string message) { using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(sql, c)) { q.Parameters.AddWithValue("@id", id); if (sql.Contains("@by")) q.Parameters.AddWithValue("@by", Admin); c.Open(); if (q.ExecuteNonQuery() == 0) return Json(new { success = false, message = "Staff record not found." }); } return Json(new { success = true, message }); }
    }
}
