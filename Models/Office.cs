namespace RiceMillProject.Models
{
    public class Office
    {
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int OfficeTypeId { get; set; }
        public string OfficeTypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class OfficeLocation
    {
        public int LocationId { get; set; }
        public int OfficeId { get; set; }
        public string LocationName { get; set; } = string.Empty;
    }

    public class OfficeType
    {
        public int OfficeTypeId { get; set; }
        public string OfficeTypeName { get; set; } = string.Empty;
    }
}
