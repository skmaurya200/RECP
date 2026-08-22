using System;
using System.Collections.Generic;

namespace Rec_Partapgarh.Models
{
    public sealed class HomeSliderContent { public string Title { get; set; } public string Description { get; set; } public string ImagePath { get; set; } }
    public sealed class HomeNoticeContent { public string Title { get; set; } public string FilePath { get; set; } public DateTime CreatedAt { get; set; } }
    public sealed class HomeEventContent { public string EventName { get; set; } public string Venue { get; set; } public DateTime EventDate { get; set; } public TimeSpan EventTime { get; set; } public string BannerImagePath { get; set; } }
    public sealed class HomeVideoContent { public string Title { get; set; } public string SourceUrl { get; set; } }
    public sealed class HomeGalleryContent { public int GalleryId { get; set; } public string CategoryName { get; set; } public string Title { get; set; } public string ImagePath { get; set; } }
    public sealed class HomeManagerContentViewModel
    {
        public List<HomeSliderContent> Sliders { get; set; } = new List<HomeSliderContent>();
        public List<HomeNoticeContent> VerticalNotices { get; set; } = new List<HomeNoticeContent>();
        public List<HomeNoticeContent> HorizontalNotices { get; set; } = new List<HomeNoticeContent>();
        public List<HomeEventContent> Events { get; set; } = new List<HomeEventContent>();
        public List<HomeVideoContent> Videos { get; set; } = new List<HomeVideoContent>();
        public List<HomeGalleryContent> Gallery { get; set; } = new List<HomeGalleryContent>();
    }
}
