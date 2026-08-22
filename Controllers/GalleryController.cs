using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers
{
    public class GalleryController : Controller
    {
        Rec_Partapgarh.Models.recpEntities av = new Rec_Partapgarh.Models.recpEntities();

        public ActionResult UploadGallery()
        {

            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            // Latest image first
            var data = av.GalleryImages.OrderByDescending(g => g.Id).ToList();
            return View(data);
        }

        [HttpPost]
        public ActionResult UploadGallery(HttpPostedFileBase ImageFile, string CategoryName)
        {
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string filename = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string path = Server.MapPath("~/images/gallery/");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ImageFile.SaveAs(Path.Combine(path, filename));

                // Check if category exists
                var existingCategory = av.Categories.FirstOrDefault(c => c.Name == CategoryName);
                int categoryId;

                if (existingCategory != null)
                {
                    categoryId = existingCategory.Id;
                }
                else
                {
                    Category newCategory = new Category { Name = CategoryName };
                    av.Categories.Add(newCategory);
                    av.SaveChanges();
                    categoryId = newCategory.Id;
                }

                GalleryImage g = new GalleryImage
                {
                    ImagePath = "/images/gallery/" + filename,
                    CategoryId = categoryId
                };

                av.GalleryImages.Add(g);
                av.SaveChanges();
                ViewBag.Message = "Image uploaded successfully.";
            }

            // Latest first
            var data = av.GalleryImages.OrderByDescending(g => g.Id).ToList();
            return View(data);
        }
        // Delete
        public ActionResult Delete(int id)
        {
            var img = av.GalleryImages.Find(id);
            if (img != null)
            {
                // Delete file also
                string fullPath = Server.MapPath(img.ImagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                av.GalleryImages.Remove(img);
                av.SaveChanges();
            }
            return RedirectToAction("UploadGallery");
        }

        // Edit
        public ActionResult Edit(int id)
        {

            if (Session["Admin"] == null)
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
            var img = av.GalleryImages.Find(id);
            return View(img);
        }

        [HttpPost]
        public ActionResult Edit(int id, string CategoryName, HttpPostedFileBase ImageFile)
        {
            var img = av.GalleryImages.Find(id);
            if (img != null)
            {
                // Category Update
                var existingCategory = av.Categories.FirstOrDefault(c => c.Name == CategoryName);
                int categoryId;
                if (existingCategory != null)
                    categoryId = existingCategory.Id;
                else
                {
                    Category newCategory = new Category { Name = CategoryName };
                    av.Categories.Add(newCategory);
                    av.SaveChanges();
                    categoryId = newCategory.Id;
                }
                img.CategoryId = categoryId;

                // Image Update
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string filename = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string path = Server.MapPath("~/images/gallery/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    ImageFile.SaveAs(Path.Combine(path, filename));
                    img.ImagePath = "/images/gallery/" + filename;
                }

                av.SaveChanges();
            }
            return RedirectToAction("UploadGallery");
        }
    }
}
