using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class SettlementBAL
    {
        private readonly SettlementDAL _settlementDal;

        public SettlementBAL(IConfiguration configuration)
        {
            _settlementDal = new SettlementDAL(configuration);
        }

        public Settlement? GetSettlementDetails(string rstNumber)
        {
            var settlement = _settlementDal.GetSettlementDetails(rstNumber);
            
            if(settlement != null && !string.IsNullOrEmpty(settlement.RSTNumber))
            {
                // Calculate Final Payable Weight
                // Formula: (Net Weight - Bag Deduction KG) - (Net Weight * Lab Deduction Pct / 100)
                
                decimal weightAfterBagDeduction = settlement.NetWeight - settlement.TotalBagDeductionKG;
                decimal labDeductionWeight = (settlement.NetWeight * settlement.LabDeductionPct) / 100.0m;
                
                settlement.FinalPayableWeight = weightAfterBagDeduction - labDeductionWeight;
            }
            
            return settlement;
        }
    }
}
