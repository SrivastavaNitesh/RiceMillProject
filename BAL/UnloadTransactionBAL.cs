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
        {
            if (unload == null)
                throw new ArgumentNullException(nameof(unload));
            if (unload.UnloadId <= 0)
                throw new ArgumentException("Invalid unloading transaction.", nameof(unload));
            if (locationId <= 0)
                throw new ArgumentException("Unloading location is required.", nameof(locationId));
            if (unload.BagTypeId.GetValueOrDefault() <= 0)
                throw new ArgumentException("Bag type is required.", nameof(unload));
            if (unload.NumberOfBags.GetValueOrDefault() <= 0)
                throw new ArgumentException("Number of bags must be greater than zero.", nameof(unload));
            if (string.IsNullOrWhiteSpace(shift))
                throw new ArgumentException("Shift is required.", nameof(shift));

            _unloadDal.CompleteWithItems(unload, locationId, shift.Trim(), workerIds ?? []);
        }
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
            if (string.IsNullOrWhiteSpace(rstNumber))
                throw new ArgumentException("RST number is required.", nameof(rstNumber));
            if (supervisorId <= 0)
                throw new ArgumentException("Supervisor is required.", nameof(supervisorId));
            if (methId <= 0)
                throw new ArgumentException("Meth is required.", nameof(methId));
            if (locationId <= 0)
                throw new ArgumentException("Unloading location is required.", nameof(locationId));

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

        public int SaveWorkerAllocation(
     int unloadId,
     int workerId,
     string workType,
     int bagTypeId,
     int bagCount,
     decimal perBagCharge)
        {
            return _unloadDal.SaveWorkerAllocation(
                unloadId,
                workerId,
                workType,
                bagTypeId,
                bagCount,
                perBagCharge
            );
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
        public void FinalizeSupervisorMethWork(
    int unloadId,
    int supervisorId)
        {
            _unloadDal.FinalizeSupervisorMethWork(
                unloadId,
                supervisorId
            );
        }
    }
}
