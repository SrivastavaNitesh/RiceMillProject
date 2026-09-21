using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class UnloadTransactionBAL
    {
        public List<Person> GetUnloadingPeople() => _unloadDal.GetUnloadingPeople();
        public void CompleteWithItems(UnloadTransaction unload, int locationId, string shift, int[] workerIds)
            => _unloadDal.CompleteWithItems(unload, locationId, shift, workerIds);
        private readonly UnloadTransactionDAL _unloadDal;

        public UnloadTransactionBAL(IConfiguration configuration)
        {
            _unloadDal = new UnloadTransactionDAL(configuration);
        }

        public List<UnloadTransaction> GetAllUnloading()
        {
            return _unloadDal.GetAllUnloading();
        }

        public int AssignUnloading(
    string rstNumber,
    int supervisorId,
    int methId,
    int? itemId,
    int locationId)
        {
            return _unloadDal.AssignUnloading(
                rstNumber,
                supervisorId,
                methId,
                itemId,
                locationId
            );
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
        public List<UnloadTransaction> GetMethAssignments(int methPersonId)
        {
            return _unloadDal.GetMethAssignments(methPersonId);
        }

        public List<Person> GetWorkersByMeth(int methPersonId)
        {
            return _unloadDal.GetWorkersByMeth(methPersonId);
        }

        public void CompleteMethUnload(
            int unloadId,
            int methPersonId,
            List<WorkerAllocation> workerRows)
        {
            _unloadDal.CompleteMethUnload(
                unloadId,
                methPersonId,
                workerRows
            );
        }
        public int SaveSupervisorUnloadAction(
    int unloadId,
    int supervisorId,
    int stackPP,
    int stackJute,
    int haudiPP,
    int haudiJute,
    List<SupervisorUnloadCategoryRow> categoryRows)
        {
            return _unloadDal.SaveSupervisorUnloadAction(
                unloadId,
                supervisorId,
                stackPP,
                stackJute,
                haudiPP,
                haudiJute,
                categoryRows
            );
        }
        public List<UnloadTransaction> GetSupervisorMethWorkRegister(
    int supervisorId)
        {
            return _unloadDal
                .GetSupervisorMethWorkRegister(
                    supervisorId
                );
        }
    }
}
