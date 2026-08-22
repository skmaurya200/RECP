using System.Collections.Generic;

namespace Rec_Partapgarh.Models
{
    public sealed class PublicGalleryCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<PublicGalleryImageViewModel> Images { get; set; } = new List<PublicGalleryImageViewModel>();
    }
    public sealed class PublicGalleryImageViewModel { public int GalleryId { get; set; } public string Title { get; set; } public string ImagePath { get; set; } }
    public sealed class PublicPressReleaseViewModel { public int PressReleaseId { get; set; } public string Title { get; set; } public string ImagePath { get; set; } }
    public sealed class PublicAcademicDocumentViewModel
    {
        public string SessionName { get; set; }
        public string CourseName { get; set; }
        public string SemesterType { get; set; }
        public string StudyYear { get; set; }
        public string FilePath { get; set; }
        public List<string> Branches { get; set; } = new List<string>();
        public string BranchNames { get { return string.Join(" / ", Branches); } }
    }
}
