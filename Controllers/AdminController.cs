using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Rec_Partapgarh.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        Rec_Partapgarh.Models.recpEntities rm = new Rec_Partapgarh.Models.recpEntities();
        // GET: Admin 
        public ActionResult Index()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            ViewBag.EnquiryCount = rm.Contacts.Count();
            ViewBag.FacultyCount = rm.faculties.Count();
            ViewBag.NotificationCount = rm.notifications.Count();
            ViewBag.DepartmentCount = rm.Departments.Count();
            return View();
        }

        public ActionResult AdminLogin()
        {

            return View();
        }
        // ======================= MEDIA COVERAGE CRUD START =======================

        public ActionResult MediaCoverage(string msg)
        {
            if (Session["Admin"] == null && Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Msg = msg;

            ViewBag.MediaCoverageList = rm.MediaCoverages
                .Where(x => x.Status == "u")
                .OrderByDescending(x => x.Id)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMediaCoverage(HttpPostedFileBase file, MediaCoverage data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Title))
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Please enter title" });
                }

                if (file == null || file.ContentLength == 0)
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Please select image" });
                }

                string fileEx = Path.GetExtension(file.FileName).ToLower();
                string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowed.Contains(fileEx))
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Only JPG, JPEG, PNG and WEBP images are allowed" });
                }

                int maxSize = 5 * 1024 * 1024;

                if (file.ContentLength > maxSize)
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Image size must be less than 5MB" });
                }

                string uploadFolder = Server.MapPath("~/Content/upload/MediaCoverage");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string customFileName = "MediaCoverage_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + fileEx;
                string path = Path.Combine(uploadFolder, customFileName);

                file.SaveAs(path);

                data.Title = data.Title.Trim();
                data.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
                data.ImagePath = customFileName;
                data.Dos = DateTime.Now;
                data.Status = "u";

                rm.MediaCoverages.Add(data);
                rm.SaveChanges();

                return RedirectToAction("MediaCoverage", "Admin", new { msg = "Media Coverage Added Successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("MediaCoverage", "Admin", new { msg = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateMediaCoverage(HttpPostedFileBase file, MediaCoverage data)
        {
            try
            {
                var oldData = rm.MediaCoverages.FirstOrDefault(x => x.Id == data.Id);

                if (oldData == null)
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Record not found" });
                }

                if (string.IsNullOrWhiteSpace(data.Title))
                {
                    return RedirectToAction("MediaCoverage", "Admin", new { msg = "Please enter title" });
                }

                oldData.Title = data.Title.Trim();
                oldData.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();

                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName).ToLower();
                    string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };

                    if (!allowed.Contains(fileEx))
                    {
                        return RedirectToAction("MediaCoverage", "Admin", new { msg = "Only JPG, JPEG, PNG and WEBP images are allowed" });
                    }

                    int maxSize = 5 * 1024 * 1024;

                    if (file.ContentLength > maxSize)
                    {
                        return RedirectToAction("MediaCoverage", "Admin", new { msg = "Image size must be less than 5MB" });
                    }

                    string uploadFolder = Server.MapPath("~/Content/upload/MediaCoverage");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    if (!string.IsNullOrEmpty(oldData.ImagePath))
                    {
                        string oldImagePath = Path.Combine(uploadFolder, oldData.ImagePath);

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string customFileName = "MediaCoverage_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + fileEx;
                    string path = Path.Combine(uploadFolder, customFileName);

                    file.SaveAs(path);

                    oldData.ImagePath = customFileName;
                }

                rm.SaveChanges();

                return RedirectToAction("MediaCoverage", "Admin", new { msg = "Media Coverage Updated Successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("MediaCoverage", "Admin", new { msg = ex.Message });
            }
        }

        public ActionResult DeleteMediaCoverage(int id)
        {
            try
            {
                var data = rm.MediaCoverages.FirstOrDefault(x => x.Id == id);

                if (data != null)
                {
                    data.Status = "d";
                    rm.SaveChanges();
                }

                return RedirectToAction("MediaCoverage", "Admin", new { msg = "Media Coverage Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("MediaCoverage", "Admin", new { msg = ex.Message });
            }
        }

        // ======================= MEDIA COVERAGE CRUD END =======================

        [HttpPost]
        public ActionResult AdminLogin(User ad)
        {
            var Msg = "";
            var data = rm.Users.FirstOrDefault(x => x.Username == ad.Username);
            if (data != null)
            {
                if (data.PasswordHash == ad.PasswordHash)
                {
                    Session["Admin"] = data.Id.ToString();
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    Msg = "Invalid Password";
                }
            }
            else
            {
                Msg = "Invalid Username";
            }

            return RedirectToAction("AdminLogin", "Admin", new { msg = Msg });
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("AdminLogin");
        }
        public ActionResult Slider(string msg)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Msg = msg;
            ViewBag.SliderImages = rm.Silders.ToList();
            return View();
        }

        // POST: Upload Slider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadSlider(HttpPostedFileBase file, Silder data)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName);
                    string[] allowed = { ".jpg", ".jpeg", ".png" };

                    if (allowed.Contains(fileEx))
                    {
                        var maxSize = 2 * 1024 * 1024; // 2 MB
                        if (file.ContentLength <= maxSize)
                        {
                            string customFileName = "Slider_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + fileEx;
                            var path = Path.Combine(Server.MapPath("~/Content/Upload/SilderImages"), customFileName);
                            file.SaveAs(path);

                            // Save to Database
                            data.Image = customFileName;
                            data.Dos = DateTime.Now;
                            rm.Silders.Add(data);
                            rm.SaveChanges();

                            return RedirectToAction("Slider", "Admin", new { msg = "File Uploaded Successfully" });
                        }
                        else
                        {
                            return RedirectToAction("Slider", "Admin", new { msg = "File size must be less than 2MB" });
                        }
                    }
                    else
                    {
                        return RedirectToAction("Slider", "Admin", new { msg = "Invalid file format" });
                    }
                }
                else
                {
                    return RedirectToAction("Slider", "Admin", new { msg = "Please select a file" });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Slider", "Admin", new { msg = ex.Message });
            }
        }

        // DELETE: Delete Slider
        public ActionResult DeleteSlider(int id)
        {
            var slider = rm.Silders.Find(id);
            if (slider != null)
            {
                var imagePath = Path.Combine(Server.MapPath("~/Content/Upload/SilderImages"), slider.Image);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                rm.Silders.Remove(slider);
                rm.SaveChanges();
            }
            return RedirectToAction("Slider", "Admin", new { msg = "Slider Deleted Successfully" });
        }
        public ActionResult notification(string msg)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "admin");
            }
            ViewBag.Msg = msg;
            ViewBag.Noti = rm.notifications.ToList();
            return View();
        }
        [HttpPost]
        public ActionResult Addnoti(notification noti)
        {
            noti.doa = DateTime.Now;
            rm.notifications.Add(noti);
            rm.SaveChanges();
            return RedirectToAction("notification", "Admin", new { msg = "Add Notification Success" });
        }
        public ActionResult DeleteNoti(int id)
        {
            var data = rm.notifications.FirstOrDefault(x => x.Id == id);
            rm.notifications.Remove(data);
            rm.SaveChanges();
            return RedirectToAction("notification");
        }
        public ActionResult Notishow()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            ViewBag.Noti = rm.notifications.ToList();
            return View();
        }
        public ActionResult DeleteNotifile(int id)
        {
            var data = rm.notificationfiles.FirstOrDefault(x => x.Id == id);
            rm.notificationfiles.Remove(data);
            rm.SaveChanges();
            return RedirectToAction("NotiFile");
        }
        public ActionResult NotiFile(string msg)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            ViewBag.NotiFile = rm.notificationfiles.ToList();
            ViewBag.Msg = msg;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNotiFile(HttpPostedFileBase file, notificationfile noti)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName);
                    string[] allowed = { ".pdf", ".doc", ".docs" };
                    if (allowed.Contains(fileEx))
                    {
                        var maxlength = 5 * 1024 * 1024;
                        if (file.ContentLength <= maxlength)
                        {

                            string customFileName = "noti" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + fileEx; // replace "myCustomFileName" with your desired file name
                            var path = Path.Combine(Server.MapPath("~/Content/upload/Notification"), customFileName);
                            file.SaveAs(path);
                            noti.file = customFileName;
                            noti.doa = DateTime.Now;
                            rm.notificationfiles.Add(noti);
                            rm.SaveChanges();
                            /*  return Content("<script>alert('Save Data'); window.location.href ='Admin/NotiFile'; </script>");*/
                            return RedirectToAction("NotiFile", "Admin", new { msg = "File Upload Sucessfully", fileName = customFileName });

                        }
                        else
                        {
                            return RedirectToAction("NotiFile", "Admin", new { msg = "Please select apt filesize equal or less than to 2mbs." });
                        }

                    }
                    else
                    {
                        return RedirectToAction("NotiFile", "Admin", new { msg = "Please select valid file" });
                    }
                }
                else
                {
                    return RedirectToAction("NotiFile", "Admin", new { msg = "Please Select a File" });
                }

            }

            catch (Exception ex)
            {
                return RedirectToAction("NotiFile", "Admin", new { msg = ex.Message });

            }


        }

        public ActionResult Gellery(string msg)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Msg = msg;
            ViewBag.Cat = rm.Galleries.ToList();
            ViewBag.GalleryIrm = rm.Galleries.Where(x => x.status == "u").ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Addgellery(HttpPostedFileBase file, Gallery data)
        {

            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName);
                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                    if (allowed.Contains(fileEx))
                    {
                        var maxlength = 10 * 1024 * 1024;
                        if (file.ContentLength <= maxlength)
                        {

                            string customFileName = "Gallery" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + fileEx; // replace "myCustomFileName" with your desired file name
                            var path = Path.Combine(Server.MapPath("~/Content/upload/Gellary"), customFileName);
                            file.SaveAs(path);
                            data.Image = customFileName;
                            data.status = "u";
                            data.dos = DateTime.Now;
                            rm.Galleries.Add(data);
                            rm.SaveChanges();

                            return RedirectToAction("Gellery", "Admin", new { msg = "File Upload Sucessfully", fileName = customFileName });
                        }
                        else
                        {
                            return RedirectToAction("Gellery", "Admin", new { msg = "Please select apt filesize equal or less than to 2mbs." });
                        }

                    }
                    else
                    {
                        return RedirectToAction("Gellery", "Admin", new { msg = "Please select valid file" });
                    }
                }
                else
                {
                    return RedirectToAction("Gellery", "Admin", new { msg = "Please Select a File" });
                }

            }

            catch (Exception ex)
            {
                return RedirectToAction("Gellery", "Admin", new { msg = ex.Message });

            }
        }
        public ActionResult ShowGellery()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            var galleryData = rm.Galleries
                .Where(x => x.status == "u")
                .ToList();

            ViewBag.GalleryImg = galleryData;

            return View();
        }

        public ActionResult DeleteIrm(int id)
        {
            var data = rm.Galleries.FirstOrDefault(x => x.Id == id);
            data.status = "d";
            rm.SaveChanges();
            return RedirectToAction("ShowGellery");
        }
        public ActionResult Department(string msg)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Msg = msg;
            ViewBag.Departments = rm.Departments.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDepartment(HttpPostedFileBase file, Department data)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName);
                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                    if (allowed.Contains(fileEx))
                    {
                        var maxSize = 5 * 1024 * 1024;
                        if (file.ContentLength <= maxSize)
                        {
                            string customFileName = "Department_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + fileEx;
                            var path = Path.Combine(Server.MapPath("~/Content/upload/Department"), customFileName);
                            file.SaveAs(path);

                            data.ImagePath = customFileName;
                            rm.Departments.Add(data);
                            rm.SaveChanges();

                            return RedirectToAction("Department", "Admin", new { msg = "Department Added Successfully" });
                        }
                        else
                        {
                            return RedirectToAction("Department", "Admin", new { msg = "File size should be less than 5MB" });
                        }
                    }
                    else
                    {
                        return RedirectToAction("Department", "Admin", new { msg = "Invalid file format" });
                    }
                }
                else
                {
                    return RedirectToAction("Department", "Admin", new { msg = "Please select a file" });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Department", "Admin", new { msg = ex.Message });
            }
        }

        public ActionResult ShowDepartment()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Departments = rm.Departments.ToList();
            return View();
        }

        public ActionResult DeleteDepartment(int id)
        {
            var data = rm.Departments.FirstOrDefault(x => x.Id == id);
            if (data != null)
            {
                rm.Departments.Remove(data);
                rm.SaveChanges();
            }
            return RedirectToAction("ShowDepartment", "Admin", new { msg = "Department Deleted Successfully" });
        }

        public ActionResult TeachingSatff(string msg)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            ViewBag.Msg = msg;
            ViewBag.TechStaff = rm.faculties.ToList();
            ViewBag.Departments = rm.Departments.Select(d => new SelectListItem
            {
                Value = d.DepartmentName,  // ✅ Database ka DepartmentName dropdown me dikhayenge
                Text = d.DepartmentName
            }).ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeachingSave(HttpPostedFileBase file, faculty data)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileEx = Path.GetExtension(file.FileName);
                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                    if (allowed.Contains(fileEx.ToLower()))
                    {
                        var maxSize = 5 * 1024 * 1024;
                        if (file.ContentLength <= maxSize)
                        {
                            string customFileName = "Faculty_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + fileEx;
                            var path = Path.Combine(Server.MapPath("~/Content/upload/Faculty"), customFileName);
                            file.SaveAs(path);

                            data.ImagePath = customFileName;
                            rm.faculties.Add(data);
                            rm.SaveChanges();

                            return RedirectToAction("ShowTeachstaff", "Admin", new { msg = "Faculty Added Successfully" });
                        }
                        else
                        {
                            return RedirectToAction("ShowTeachstaff", "Admin", new { msg = "File size should be under 5MB" });
                        }
                    }
                    else
                    {
                        return RedirectToAction("ShowTeachstaff", "Admin", new { msg = "Invalid file format" });
                    }
                }
                else
                {
                    return RedirectToAction("ShowTeachstaff", "Admin", new { msg = "Please upload an image" });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowTeachstaff", "Admin", new { msg = ex.Message });
            }
        }

        public ActionResult ShowTeachstaff()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            var facultyList = rm.faculties.ToList(); // apne DbContext ke hisaab se change karein
            ViewBag.FacultyList = facultyList;
            return View();

        }


        public ActionResult Deletechsatff(int id)
        {
            var data = rm.faculties.FirstOrDefault(x => x.Id == id);
            if (data != null)
            {
                rm.faculties.Remove(data);
                rm.SaveChanges();
            }
            return RedirectToAction("ShowTeachstaff", "Admin", new { msg = "Department Deleted Successfully" });
        }

        //==========================================================================================================//

        // Database Context
        private recpEntities rmp = new recpEntities();
        private List<string> allowedTables = new List<string> { "Memorandum_of_Association", "G_O_for_Change", "AICTE", "Minutes_of_Meeting", "Society_Bylaws", "List_of_Holidays", "Academic_Committee", "Mous", "IQAC" };
        public ActionResult masterentry()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var allTables = rm.Database.SqlQuery<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'").ToList();
            ViewBag.TableNames = allTables.Where(t => allowedTables.Contains(t)).ToList(); // ✅ Filter only allowed tables
            return View();
        }

        [HttpPost]
        public ActionResult SubmitData(HttpPostedFileBase file, string SelectedTable, string Name)
        {
            try
            {
                if (file == null || file.ContentLength == 0 || string.IsNullOrEmpty(SelectedTable))
                {
                    return RedirectToAction("masterentry", new { msg = "Please select a table and upload a file." });
                }

                string fileExt = Path.GetExtension(file.FileName);
                string[] allowed = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" }; // Allowed file types
                if (!allowed.Contains(fileExt))
                {
                    return RedirectToAction("masterentry", new { msg = "Invalid file format." });
                }

                string customFileName = "File_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + fileExt;
                string path = Path.Combine(Server.MapPath("~/Content/upload/Document"), customFileName);
                file.SaveAs(path);

                // Data insert using SQL Query
                string query = $"INSERT INTO {SelectedTable} (Name, FilePath) VALUES (@p0, @p1)";
                rm.Database.ExecuteSqlCommand(query, Name, customFileName);

                return RedirectToAction("masterentry", new { msg = "File uploaded successfully!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("masterentry", new { msg = ex.Message });
            }
        }

        public ActionResult Success(string msg)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            ViewBag.Message = msg;
            return View();
        }
        public ActionResult showmasterentry(string SelectedTable)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            ViewBag.TableNames = allowedTables; // ✅ Sirf allowed tables dropdown me dikhengi
            ViewBag.SelectedTable = SelectedTable; // ✅ User ka selected table pass karna

            if (!string.IsNullOrEmpty(SelectedTable))
            {
                // ✅ Agar table selected hai, toh uska data fetch karo
                ViewBag.TableData = GetTableData(SelectedTable);
            }

            return View();
        }

        // ✅ **Function: Get Table Data**
        private DataTable GetTableData(string tableName)
        {
            DataTable dt = new DataTable();
            try
            {
                string query = $"SELECT * FROM {tableName}";
                using (var cmd = rm.Database.Connection.CreateCommand())
                {
                    cmd.CommandText = query;
                    rm.Database.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                    rm.Database.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }
            return dt;
        }

        public ActionResult DeleteRecord(string tableName, int id)
        {
            try
            {
                if (string.IsNullOrEmpty(tableName) || id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid request!";
                    return RedirectToAction("showmasterentry");
                }

                if (!allowedTables.Contains(tableName))
                {
                    TempData["ErrorMessage"] = "You are not allowed to delete from this table!";
                    return RedirectToAction("showmasterentry", new { SelectedTable = tableName });
                }

                string query = $"DELETE FROM {tableName} WHERE Id = @Id"; // ✅ Dynamic Delete Query
                using (var cmd = rm.Database.Connection.CreateCommand())
                {
                    rm.Database.Connection.Open();
                    cmd.CommandText = query;
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", id));
                    cmd.ExecuteNonQuery();
                    rm.Database.Connection.Close();
                }

                TempData["SuccessMessage"] = "Record deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting record: " + ex.Message;
            }

            return RedirectToAction("showmasterentry", new { SelectedTable = tableName });
        }

        public ActionResult Acadmicshow()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var calendars = rm.AcademicCalendars.ToList();
            return View(calendars);
        }

        // CREATE: Show Create Form
        public ActionResult adcadmicCreate()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            return View();
        }

        // CREATE: Save Data
        [HttpPost]
        public ActionResult adcadmicCreate(AcademicCalendar calendar, HttpPostedFileBase File)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (File != null && File.ContentLength > 0)
                    {
                        string filePath = Path.Combine(Server.MapPath("~/Content/upload/Adacdmic"), Path.GetFileName(File.FileName));
                        File.SaveAs(filePath);
                        calendar.FilePath = "~/Content/upload/Adacdmic/" + File.FileName;
                    }

                    rm.AcademicCalendars.Add(calendar);
                    rm.SaveChanges();
                    TempData["SuccessMessage"] = "Academic Calendar Created Successfully!";
                    return RedirectToAction("adcadmicCreate");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error Occurred: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Validation Failed! Please check inputs.";
            }
            return View(calendar);
        }



        public ActionResult acadmicEdit(int id)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            if (id == 0) return HttpNotFound();

            var calendar = rm.AcademicCalendars.Find(id);
            if (calendar == null) return HttpNotFound();

            return View(calendar);
        }

        // ✅ EDIT: Save Edited Data
        [HttpPost]
        public ActionResult acadmicEdit(AcademicCalendar calendar, HttpPostedFileBase File)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data! Please check the input.";
                return View(calendar);
            }

            var existingCalendar = rm.AcademicCalendars.Find(calendar.Id);
            if (existingCalendar == null) return HttpNotFound();

            try
            {
                // ✅ File Upload Fix: Agar naya file upload ho raha hai tabhi overwrite ho
                if (File != null && File.ContentLength > 0)
                {
                    string filePath = Path.Combine(Server.MapPath("~/Content/upload/Adacdmic"), Path.GetFileName(File.FileName));

                    // ✅ Purana file delete karna hai agar naya file upload ho raha hai
                    if (!string.IsNullOrEmpty(existingCalendar.FilePath))
                    {
                        string oldFilePath = Server.MapPath(existingCalendar.FilePath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // ✅ Naya file save karo
                    File.SaveAs(filePath);
                    existingCalendar.FilePath = "/Content/upload/Adacdmic/" + File.FileName;
                }

                // ✅ Title aur Semester Type Update Karo
                existingCalendar.Title = calendar.Title;
                existingCalendar.SemesterType = calendar.SemesterType;

                // ✅ Save Changes
                rm.SaveChanges();

                // ✅ Success Message
                TempData["SuccessMessage"] = "Academic Calendar Updated Successfully!";
                return RedirectToAction("Acadmicshow");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error occurred: " + ex.Message;
                return View(calendar);
            }
        }


        // ✅ DELETE: Show Delete Confirmation
        public ActionResult adcdmicDelete(int id)
        {
            var data = rm.AcademicCalendars.FirstOrDefault(x => x.Id == id);
            if (data != null)
            {
                rm.AcademicCalendars.Remove(data);
                rm.SaveChanges();
            }
            return RedirectToAction("Acadmicshow", "Admin", new { msg = "Department Deleted Successfully" });
        }

        public ActionResult ContactMessages()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var contacts = rm.Contacts.OrderByDescending(c => c.SubmittedAt).ToList();
            return View(contacts);
        }

        // ✅ Contact Message Delete Karna
        public ActionResult DeleteContact(int id)
        {
            var contact = rm.Contacts.Find(id);
            if (contact != null)
            {
                rm.Contacts.Remove(contact);
                rm.SaveChanges();
                TempData["SuccessMessage"] = "Message deleted successfully!";
            }
            return RedirectToAction("ContactMessages");
        }
        public ActionResult GrievanceList()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var grievances = rm.Grievances.OrderByDescending(g => g.SubmittedAt).ToList();
            return View(grievances);
        }

        public ActionResult DeleteGrievance(int id)
        {
            var grievance = rm.Grievances.Find(id);
            if (grievance != null)
            {
                rm.Grievances.Remove(grievance);
                rm.SaveChanges();
                TempData["SuccessMessage"] = "Grievance deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Grievance not found!";
            }
            return RedirectToAction("GrievanceList");
        }

        //=====================================================this is notification=============//


        private List<string> allowedTables1 = new List<string> { "Events", "etenders", "Circulars" };
        public ActionResult MasterNotices()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            // Controller
var tableMapping = new Dictionary<string, string>
{
    { "Events", "Upcoming Events" },
    { "Circulars", "Vertical Notice" }
};

// Filter only allowed tables
ViewBag.TableNames = tableMapping.Where(t => allowedTables1.Contains(t.Key)).ToList();
 // ✅ Filter only allowed tables
            return View();
        }

        [HttpPost]
        public ActionResult SubmitData1(HttpPostedFileBase file, string SelectedTable, string notification, string urllink, DateTime? eventDate)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(notification))
                {
                    return RedirectToAction("MasterNotices", new { msg = "Please select a table and enter a notification." });
                }

                string customFileName = null;

                // ✅ File Upload if Provided
                if (file != null && file.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(file.FileName).ToLower();
                    string[] allowed = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

                    if (!allowed.Contains(fileExt))
                    {
                        return RedirectToAction("MasterNotices", new { msg = "Invalid file format." });
                    }

                    customFileName = "File_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + fileExt;
                    string path = Path.Combine(Server.MapPath("~/Content/upload/Document"), customFileName);
                    file.SaveAs(path);
                }

                // ✅ Insert Data (Allow NULL for file & urllink & eventDate)
                string query;
                if (SelectedTable == "Events" && eventDate.HasValue)
                {
                    query = $"INSERT INTO {SelectedTable} (notification, [file], urllink, doa) VALUES (@p0, @p1, @p2, @p3)";
                    rm.Database.ExecuteSqlCommand(query, notification, (object)customFileName ?? DBNull.Value, (object)urllink ?? DBNull.Value, eventDate.Value);
                }
                else
                {
                    query = $"INSERT INTO {SelectedTable} (notification, [file], urllink, doa) VALUES (@p0, @p1, @p2, @p3)";
                    rm.Database.ExecuteSqlCommand(query, notification, (object)customFileName ?? DBNull.Value, (object)urllink ?? DBNull.Value, DateTime.Now);
                }

                return RedirectToAction("MasterNotices", new { msg = "Data saved successfully!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("MasterNotices", new { msg = "Error: " + ex.Message });
            }
        }



        private List<string> allowedTables2 = new List<string> { "Events", "etenders", "Circulars" };
        private DataTable GetTableData1(string tableName)
        {
            DataTable dt = new DataTable();
            try
            {
                string query = $"SELECT Id, notification, urllink, [file], doa FROM {tableName} ORDER BY doa DESC";
                using (var cmd = rm.Database.Connection.CreateCommand())
                {
                    cmd.CommandText = query;
                    rm.Database.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                    rm.Database.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }
            return dt;
        }

        public ActionResult ShowMasterEntrynoti(string SelectedTable)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }

            // Allowed table lists
            var allowedTables1 = new List<string> { "Events", "Circulars" };
            var allowedTables2 = new List<string> { "Events", "Circulars" };

            // Table mapping (Key = TableName, Value = DisplayName)
            var tableMapping = new Dictionary<string, string>
    {
        { "Events", "Upcoming Events" },
        { "Circulars", "Vertical Notice" }
    };

            // Filter only allowed tables and send as list of KeyValuePairs
            ViewBag.TableNames = tableMapping
                .Where(t => allowedTables1.Contains(t.Key))
                .ToList();

            ViewBag.SelectedTable = SelectedTable;

            if (!string.IsNullOrEmpty(SelectedTable) && allowedTables2.Contains(SelectedTable))
            {
                ViewBag.TableData = GetTableData1(SelectedTable);
            }
            else
            {
                ViewBag.TableData = null;
            }

            return View();
        }

        public ActionResult DeleteRecordnotice(string tableName, int id)
        {
            try
            {
                // ✅ Sirf allowedTables2 me hi delete allowed hai
                if (!allowedTables2.Contains(tableName))
                {
                    return RedirectToAction("ShowMasterEntrynoti", new { msg = "Invalid Table Selected." });
                }

                // ✅ SQL Query to Delete Record
                string query = $"DELETE FROM {tableName} WHERE Id = @p0";
                rm.Database.ExecuteSqlCommand(query, id);

                return RedirectToAction("ShowMasterEntrynoti", new { SelectedTable = tableName, msg = "Record Deleted Successfully!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowMasterEntrynoti", new { SelectedTable = tableName, msg = ex.Message });
            }
        }

        /* time table*/

        public ActionResult AddTimeTable()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddTimeTable(TimeTable t, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string fileName = Path.GetFileName(file.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/Uploads/TimeTables/"), fileName);
                file.SaveAs(path);
                t.FilePath = "~/Content/Uploads/TimeTables/" + fileName;
            }

            rm.TimeTables.Add(t);
            rm.SaveChanges();

            TempData["msg"] = "TimeTable uploaded successfully!";
            return RedirectToAction("AddTimeTable");
        }

        public ActionResult ViewTimeTables()
        {
            return View(rm.TimeTables.ToList());
        }

        public ActionResult DeleteTimeTable(int id)
        {
            var t = rm.TimeTables.Find(id);
            if (t != null)
            {
                rm.TimeTables.Remove(t);
                rm.SaveChanges();
            }
            return RedirectToAction("ViewTimeTables");
        }
        public ActionResult AddActivity()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AddActivity(RecentActivity r, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {
                string filename = Path.GetFileName(img.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/uploads/recent"), filename);
                img.SaveAs(path);
                r.ImagePath = "~/Content/uploads/recent" + filename;
            }

            rm.RecentActivities.Add(r);
            rm.SaveChanges();
            TempData["msg"] = "Activity Uploaded Successfully!";
            return RedirectToAction("AddActivity");
        }
    }
}