namespace Rec_Partapgarh.Models
{
    public class ManagerTimeTable
    {
        public int TimeTableId { get; set; }
        public string SessionName { get; set; }
        public string CourseName { get; set; }
        public string SemesterType { get; set; }
        public string StudyYear { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string BranchNames { get; set; }
        public string FilePath { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
