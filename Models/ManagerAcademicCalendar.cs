namespace Rec_Partapgarh.Models
{
    public class ManagerAcademicCalendar
    {
        public int AcademicCalendarId { get; set; }
        public string Title { get; set; }
        public string SemesterType { get; set; }
        public string FilePath { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
