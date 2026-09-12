using System;
using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class LocationMaster
    {
        public int LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string? LocationCode { get; set; }

        [Required(ErrorMessage = "Location Name is required")]
        [Display(Name = "Location Name")]
        public string LocationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location Type is required")]
        [Display(Name = "Location Type")]
        public string LocationType { get; set; } = string.Empty;

        [Display(Name = "Capacity")]
        public decimal Capacity { get; set; }

        [Display(Name = "Capacity Unit")]
        public string? CapacityUnit { get; set; } = "MT";

        [Display(Name = "Location Description")]
        public string? Description { get; set; }

        [Display(Name = "Area / Address")]
        public string? Address { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;
    }

    public class OfficeLocationMapping
    {
        public int MappingId { get; set; }

        [Required(ErrorMessage = "Office selection is required")]
        [Display(Name = "Office")]
        public int OfficeId { get; set; }

        public string? OfficeName { get; set; }

        [Required(ErrorMessage = "Location selection is required")]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        public string? LocationName { get; set; }
        public string? LocationCode { get; set; }

        [Display(Name = "Unloading Allowed")]
        public bool UnloadingAllowed { get; set; } = true;

        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;
    }
}
