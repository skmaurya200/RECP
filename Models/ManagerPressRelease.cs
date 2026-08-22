namespace Rec_Partapgarh.Models
{
    public class ManagerPressRelease
    {
        public int PressReleaseId { get; set; }
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
