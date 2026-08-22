using Rec_Partapgarh.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Rec_Partapgarh.Controllers.API
{
    public class DepartmentApiController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet]
        public JsonResult List()
        {
            var departments = new List<ManagerDepartment>();

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"SELECT DepartmentId, DepartmentName, IsActive, CreatedAt, UpdatedAt
                                                  FROM dbo.tbl_Department
                                                  ORDER BY DepartmentId DESC", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departments.Add(new ManagerDepartment
                        {
                            DepartmentId = reader.GetInt32(0),
                            DepartmentName = reader.GetString(1),
                            IsActive = reader.GetBoolean(2),
                            CreatedAt = reader.GetDateTime(3),
                            UpdatedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                        });
                    }
                }
            }

            return Json(new { success = true, data = departments }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Save(ManagerDepartment model)
        {
            model.DepartmentName = (model.DepartmentName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(model.DepartmentName) || model.DepartmentName.Length > 150)
                return Json(new { success = false, message = "Please correct the validation errors.", errors = new { DepartmentName = "Valid department name is required (maximum 150 characters)." } });

            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    if (model.DepartmentId == 0)
                    {
                        command.CommandText = @"INSERT INTO dbo.tbl_Department (DepartmentName, IsActive, CreatedBy)
                                                VALUES (@DepartmentName, @IsActive, 'superAdmin')";
                    }
                    else
                    {
                        command.CommandText = @"UPDATE dbo.tbl_Department
                                                SET DepartmentName = @DepartmentName,
                                                    IsActive = @IsActive,
                                                    UpdatedAt = SYSDATETIME(), UpdatedBy = 'superAdmin'
                                                WHERE DepartmentId = @DepartmentId";
                        command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = model.DepartmentId;
                    }

                    command.Parameters.Add("@DepartmentName", SqlDbType.NVarChar, 150).Value = model.DepartmentName;
                    command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = model.IsActive;
                    connection.Open();
                    var affected = command.ExecuteNonQuery();

                    if (model.DepartmentId > 0 && affected == 0)
                        return Json(new { success = false, message = "Department not found." });
                }

                return Json(new
                {
                    success = true,
                    message = model.DepartmentId == 0 ? "Department added successfully." : "Department updated successfully."
                });
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return Json(new { success = false, message = "Department name already exists.", errors = new { DepartmentName = "Department name already exists." } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleStatus(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"UPDATE dbo.tbl_Department
                                                  SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END,
                                                      UpdatedAt = SYSDATETIME(), UpdatedBy = 'superAdmin'
                                                  WHERE DepartmentId = @DepartmentId", connection))
            {
                command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = id;
                connection.Open();
                if (command.ExecuteNonQuery() == 0)
                    return Json(new { success = false, message = "Department not found." });
            }

            return Json(new { success = true, message = "Department status updated." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("DELETE FROM dbo.tbl_Department WHERE DepartmentId = @DepartmentId", connection))
            {
                command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = id;
                connection.Open();
                if (command.ExecuteNonQuery() == 0)
                    return Json(new { success = false, message = "Department not found." });
            }

            return Json(new { success = true, message = "Department deleted successfully." });
        }
    }
}
