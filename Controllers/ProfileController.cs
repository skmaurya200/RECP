using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers
{
    public class ProfileController : Controller
    {
        recpEntities db = new recpEntities();

        // List of faculties
        public ActionResult Faculties()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var list = db.profiles.OrderBy(f => f.SortOrder).ToList();
            return View(list);
        }

        // Individual faculty profile
        public ActionResult FacultyProfile(int? id)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var faculty = db.profiles.Find(id);
            if (faculty == null) return HttpNotFound();
            return View(faculty);
        }

        // GET: Create Faculty
        public ActionResult CreateFaculty()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            return View();
        }

        // POST: Create Faculty
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult CreateFaculty(profile faculty, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // Image upload
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadsDir = Server.MapPath("~/Content/img/faculty/");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = Path.GetFileName(imageFile.FileName);
                    var uniq = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + fileName;
                    var path = Path.Combine(uploadsDir, uniq);
                    imageFile.SaveAs(path);

                    faculty.ImageUrl = Url.Content(Path.Combine("~/Content/img/faculty/", uniq));
                }

                faculty.CreatedAt = DateTime.Now;

                // Save Complete Profile HTML
                faculty.CompleteProfileHtml = faculty.CompleteProfileHtml;

                db.profiles.Add(faculty);
                db.SaveChanges();
                return RedirectToAction("Faculties");
            }
            return View(faculty);
        }

        // GET: Edit Faculty
        public ActionResult EditFaculty(int? id)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var faculty = db.profiles.Find(id);
            if (faculty == null) return HttpNotFound();
            return View(faculty);
        }

        // POST: Edit Faculty
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditFaculty(profile faculty, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                var dbFaculty = db.profiles.Find(faculty.Id);
                if (dbFaculty == null) return HttpNotFound();

                dbFaculty.FullName = faculty.FullName;
                dbFaculty.Designation = faculty.Designation;
                dbFaculty.Email = faculty.Email;
                dbFaculty.Mobile = faculty.Mobile;
                dbFaculty.ProfileUrl = faculty.ProfileUrl;
                dbFaculty.SortOrder = faculty.SortOrder;
                dbFaculty.IsActive = faculty.IsActive;

                // Update Complete Profile HTML
                dbFaculty.CompleteProfileHtml = faculty.CompleteProfileHtml;

                // Update Image
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadsDir = Server.MapPath("~/Content/img/faculty/");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = Path.GetFileName(imageFile.FileName);
                    var uniq = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + fileName;
                    var path = Path.Combine(uploadsDir, uniq);
                    imageFile.SaveAs(path);

                    dbFaculty.ImageUrl = Url.Content(Path.Combine("~/Content/img/faculty/", uniq));
                }

                db.SaveChanges();
                return RedirectToAction("Faculties");
            }
            return View(faculty);
        }

        // GET: Delete Faculty
        public ActionResult DeleteFaculty(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var faculty = db.profiles.Find(id);
            if (faculty == null) return HttpNotFound();
            return View(faculty);
        }

        // POST: Delete Faculty
        [HttpPost, ActionName("DeleteFaculty")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var faculty = db.profiles.Find(id);
            if (faculty != null)
            {
                db.profiles.Remove(faculty);
                db.SaveChanges();
            }
            return RedirectToAction("Faculties");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
