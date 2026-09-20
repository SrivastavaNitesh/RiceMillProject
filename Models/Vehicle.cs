using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Vehicle number is required.")]
        [StringLength(30, ErrorMessage = "Vehicle number cannot exceed 30 characters.")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Party / Supplier is required.")]
        public int PartyId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Default Driver is required.")]
        public int DriverId { get; set; }

        public bool IsActive { get; set; }
    }
}
