using System;

namespace Rec_Partapgarh.Models
{
    public class ManagerGeneralNotice { public int NoticeId { get; set; } public string NoticeType { get; set; } public string Title { get; set; } public string FilePath { get; set; } public bool IsActive { get; set; } public string CreatedBy { get; set; } public string UpdatedBy { get; set; } }
    public class ManagerEvent { public int EventId { get; set; } public string EventName { get; set; } public string Venue { get; set; } public DateTime EventDate { get; set; } public TimeSpan EventTime { get; set; } public string BannerImagePath { get; set; } public bool IsActive { get; set; } public string CreatedBy { get; set; } public string UpdatedBy { get; set; } }
    public class ManagerVideo { public int VideoId { get; set; } public string Title { get; set; } public string SourceUrl { get; set; } public bool IsActive { get; set; } public string CreatedBy { get; set; } public string UpdatedBy { get; set; } }
}
