using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class LabQualityCheckBAL
    {
        private readonly LabQualityCheckDAL _labDal;

        public LabQualityCheckBAL(IConfiguration configuration)
        {
            _labDal = new LabQualityCheckDAL(configuration);
        }

        public bool SaveLabQualityCheck(LabQualityCheck labCheck)
        {
            // Business logic for deductions based on limit:
            // Assuming standard limits: Moisture 14%, Dust 1%, Payia 1%, Grain 2%
            decimal totalDeduction = 0;
            
            if(labCheck.MoisturePct > 14.0m) totalDeduction += (labCheck.MoisturePct - 14.0m) * 1.5m; // Penalty multiplier
            if(labCheck.DustPct > 1.0m) totalDeduction += (labCheck.DustPct - 1.0m) * 1.0m;
            if(labCheck.PayiaPct > 1.0m) totalDeduction += (labCheck.PayiaPct - 1.0m) * 1.0m;
            
            labCheck.TotalDeductionPct = totalDeduction;

            return _labDal.SaveLabQualityCheck(labCheck);
        }
    }
}
