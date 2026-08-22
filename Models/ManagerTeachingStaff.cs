using System.Web.Mvc;

namespace Rec_Partapgarh.Models
{
    public class ManagerTeachingStaff
    {
        public int StaffId { get; set; }
        public string PhotoPath { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AlternateEmail { get; set; }
        public string MobileNumber { get; set; }
        public string LandlineNumber { get; set; }
        public string Designation { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Qualification { get; set; }
        public int? DisplayOrder { get; set; }
        [AllowHtml]
        public string LongDescription { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
