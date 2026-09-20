using System.ComponentModel.DataAnnotations;

namespace RiceMillProject.Models
{
    public class Person
    {
        public int PersonId { get; set; }
        
        [Required(ErrorMessage = "Person Name is required.")]
        public string PersonName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;
        
        public string? Address { get; set; }
        [Required(ErrorMessage = "Employee Role is required.")]
        public string PersonType { get; set; } = string.Empty;
        
        public int PartyCategory { get; set; } = 1; // 1: Direct Party, 2: Center Party
        public string? FirmName { get; set; }
        public string? FirmDesignation { get; set; }
        
        public bool IsActive { get; set; } = true;
        public int OfficeId { get; set; }
        public int? GateId { get; set; }
        public string? GateName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int DesignationId { get; set; }
        public int? MethdesignatationId { get; set; }
        public int PostId { get; set; }
        public string? EmployeeCode { get; set; }
    }
}
