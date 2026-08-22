using System;
using System.ComponentModel.DataAnnotations;

namespace Rec_Partapgarh.Models
{
    public class ManagerDepartment
    {
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(150)]
        public string DepartmentName { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
