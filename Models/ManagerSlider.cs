using System;
using System.ComponentModel.DataAnnotations;

namespace Rec_Partapgarh.Models
{
    public class ManagerSlider
    {
        public int SliderId { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; }

        [Required, StringLength(500)]
        public string SortDescription { get; set; }

        [StringLength(300)]
        public string ImagePath { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
