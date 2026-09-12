using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class UnloadTransactionBAL
    {
        private readonly UnloadTransactionDAL _unloadDal;

        public UnloadTransactionBAL(IConfiguration configuration)
        {
            _unloadDal = new UnloadTransactionDAL(configuration);
        }

        public List<UnloadTransaction> GetAllUnloading()
        {
            return _unloadDal.GetAllUnloading();
        }

        public int AssignUnloading(string rstNumber, int supervisorId, int methId)
        {
            return _unloadDal.AssignUnloading(rstNumber, supervisorId, methId);
        }

        public bool SubmitUnloading(int unloadId, int gateManId, int bagTypeId, int numberOfBags)
        {
            return _unloadDal.SubmitUnloading(unloadId, gateManId, bagTypeId, numberOfBags);
        }

        public bool VerifyUnloading(int unloadId, decimal totalBagDeductionGrams)
        {
            return _unloadDal.VerifyUnloading(unloadId, totalBagDeductionGrams);
        }

        public void SaveUnloadLocation(int unloadId, int locationId)
        {
            _unloadDal.SaveUnloadLocation(unloadId, locationId);
        }

        public bool SaveWorkerAllocation(int unloadId, int workerId, decimal palledariAmount)
        {
            return _unloadDal.SaveWorkerAllocation(unloadId, workerId, palledariAmount);
        }
    }
}
