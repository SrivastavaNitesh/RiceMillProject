namespace RiceMillProject.Models
{
    public class BagType
    {
        public int BagTypeId { get; set; }
        public string BagTypeName { get; set; } = string.Empty;
        public int DeductionWeightGrams { get; set; }
        public bool IsActive { get; set; }
    }
}
