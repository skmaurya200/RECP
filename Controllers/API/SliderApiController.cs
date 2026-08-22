using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class SliderApiController : Controller
    {
        private const int MaxImageBytes = 3 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet]
        public JsonResult List()
        {
            var sliders = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"SELECT SliderId, Title, SortDescription, ImagePath, IsActive
                                                  FROM dbo.tbl_Slider ORDER BY SliderId DESC", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var imagePath = reader.GetString(3);
                        sliders.Add(new
                        {
                            SliderId = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            SortDescription = reader.GetString(2),
                            ImagePath = imagePath,
                            ImageUrl = Url.Content(imagePath),
                            IsActive = reader.GetBoolean(4)
                        });
                    }
                }
            }
            return Json(new { success = true, data = sliders }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Save(ManagerSlider model)
        {
            model.Title = (model.Title ?? string.Empty).Trim();
            model.SortDescription = (model.SortDescription ?? string.Empty).Trim();
            var image = Request.Files["ImageFile"];
            var errors = ValidateSlider(model, image);
            if (errors.Count > 0) return Json(new { success = false, message = "Please correct the validation errors.", errors });

            string oldImagePath = null;
            string newImagePath = null;
            try
            {
                if (model.SliderId > 0) oldImagePath = GetImagePath(model.SliderId);
                if (model.SliderId > 0 && oldImagePath == null)
                    return Json(new { success = false, message = "Slider not found." });

                if (image != null && image.ContentLength > 0)
                    newImagePath = SaveImage(image);

                var finalImagePath = newImagePath ?? oldImagePath;
                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    if (model.SliderId == 0)
                    {
                        command.CommandText = @"INSERT INTO dbo.tbl_Slider (Title, SortDescription, ImagePath, IsActive, CreatedBy)
                                                VALUES (@Title, @SortDescription, @ImagePath, @IsActive, 'superAdmin')";
                    }
                    else
                    {
                        command.CommandText = @"UPDATE dbo.tbl_Slider SET Title=@Title, SortDescription=@SortDescription,
                                                ImagePath=@ImagePath, IsActive=@IsActive, UpdatedAt=SYSDATETIME(), UpdatedBy='superAdmin'
                                                WHERE SliderId=@SliderId";
                        command.Parameters.Add("@SliderId", SqlDbType.Int).Value = model.SliderId;
                    }
                    command.Parameters.Add("@Title", SqlDbType.NVarChar, 150).Value = model.Title;
                    command.Parameters.Add("@SortDescription", SqlDbType.NVarChar, 500).Value = model.SortDescription;
                    command.Parameters.Add("@ImagePath", SqlDbType.NVarChar, 300).Value = finalImagePath;
                    command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = model.IsActive;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                if (newImagePath != null && oldImagePath != null) DeleteImage(oldImagePath);
                return Json(new { success = true, message = model.SliderId == 0 ? "Slider added successfully." : "Slider updated successfully." });
            }
            catch
            {
                if (newImagePath != null) DeleteImage(newImagePath);
                return Json(new { success = false, message = "Unable to save slider. Please try again." });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult ToggleStatus(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"UPDATE dbo.tbl_Slider SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,
                                                  UpdatedAt=SYSDATETIME(), UpdatedBy='superAdmin' WHERE SliderId=@SliderId", connection))
            {
                command.Parameters.Add("@SliderId", SqlDbType.Int).Value = id;
                connection.Open();
                if (command.ExecuteNonQuery() == 0) return Json(new { success = false, message = "Slider not found." });
            }
            return Json(new { success = true, message = "Slider status updated." });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            var imagePath = GetImagePath(id);
            if (imagePath == null) return Json(new { success = false, message = "Slider not found." });
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("DELETE FROM dbo.tbl_Slider WHERE SliderId=@SliderId", connection))
            {
                command.Parameters.Add("@SliderId", SqlDbType.Int).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }
            DeleteImage(imagePath);
            return Json(new { success = true, message = "Slider deleted successfully." });
        }

        private Dictionary<string, string> ValidateSlider(ManagerSlider model, HttpPostedFileBase image)
        {
            var errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(model.Title)) errors["Title"] = "Title is required.";
            else if (model.Title.Length > 150) errors["Title"] = "Title cannot exceed 150 characters.";
            if (string.IsNullOrWhiteSpace(model.SortDescription)) errors["SortDescription"] = "Sort description is required.";
            else if (model.SortDescription.Length > 500) errors["SortDescription"] = "Sort description cannot exceed 500 characters.";
            if (model.SliderId == 0 && (image == null || image.ContentLength == 0)) errors["ImageFile"] = "Slider image is required.";
            if (image != null && image.ContentLength > 0)
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains((image.ContentType ?? string.Empty).ToLowerInvariant())) errors["ImageFile"] = "Only JPG, JPEG, PNG or WEBP images are allowed.";
                else if (image.ContentLength > MaxImageBytes) errors["ImageFile"] = "Image size cannot exceed 3 MB.";
            }
            return errors;
        }

        private string SaveImage(HttpPostedFileBase image)
        {
            var folder = Server.MapPath("~/Content/uploads/manager/sliders");
            Directory.CreateDirectory(folder);
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(image.FileName).ToLowerInvariant();
            image.SaveAs(Path.Combine(folder, fileName));
            return "~/Content/uploads/manager/sliders/" + fileName;
        }

        private string GetImagePath(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("SELECT ImagePath FROM dbo.tbl_Slider WHERE SliderId=@SliderId", connection))
            {
                command.Parameters.Add("@SliderId", SqlDbType.Int).Value = id;
                connection.Open();
                return command.ExecuteScalar() as string;
            }
        }

        private void DeleteImage(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath) || !virtualPath.StartsWith("~/Content/uploads/manager/sliders/", StringComparison.OrdinalIgnoreCase)) return;
            var path = Server.MapPath(virtualPath);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
