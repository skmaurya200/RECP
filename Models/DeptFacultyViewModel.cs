using System.Collections.Generic;

namespace Rec_Partapgarh.Models
{
    public class DeptFacultyViewModel
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string SidebarPath { get; set; }
        public List<PublicFacultyViewModel> Faculty { get; set; }
    }
}
