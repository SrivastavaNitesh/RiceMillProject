using RiceMillProject.DAL;
using System.Data;

namespace RiceMillProject.Models
{
    public class BusinessLayer
    {
        private readonly DataLayer _objDal;

        public BusinessLayer(IConfiguration configuration)
        {
            _objDal = new DataLayer(configuration);
        }

        internal bool AdddhermKata(DhermKata obj)
        {
            return _objDal.InsertDhermkataDetails(obj);
        }

        internal DataTable GetAlldhermkata() => _objDal.GetAlldhermkata();

        internal DataTable GetAllMainoffice() => _objDal.GetAllMainoffice();
        internal DataTable GetEmployeePost() => _objDal.GetEmployeePost();
        internal DataTable GetPost() => _objDal.GetPost();
        internal DataTable GetAllMeth() => _objDal.GetAllMeth();
        internal DataTable GetWorkersByMeth(int methPersonId) => _objDal.GetWorkersByMeth(methPersonId);
        internal DataTable GetActiveInwardEntriesForDropdown() => _objDal.GetActiveInwardEntriesForDropdown();
        internal DataTable CreateGatemanInward(string vehicleNumber, string driverName, int gateManId, int? itemId = null, decimal? quantity = null, string? unit = null) 
            => _objDal.CreateGatemanInward(vehicleNumber, driverName, gateManId, itemId, quantity, unit);
        internal DataTable GetChallanDetailsForOutward(string challanNo) => _objDal.GetChallanDetailsForOutward(challanNo);
        internal DataTable ApproveOutwardChallan(string challanNo, int gateManId) => _objDal.ApproveOutwardChallan(challanNo, gateManId);

        // --- NEW PDF GATE MANAGEMENT MODULE BUSINESS METHODS ---
        internal DataTable ManageGateMaster(string action, GateMaster? gate = null) => _objDal.ManageGateMaster(action, gate);
        internal DataTable ManageDepartmentMaster(string action, DepartmentMaster? dept = null) => _objDal.ManageDepartmentMaster(action, dept);
        internal DataTable CreateVisitorEntry(VisitorRegister v, int createdBy) => _objDal.CreateVisitorEntry(v, createdBy);
        internal void ExitVisitor(string visitorNo) => _objDal.ExitVisitor(visitorNo);
        internal DataTable CreateTempMaterialEntry(TempMaterialRegister item, int createdBy) => _objDal.CreateTempMaterialEntry(item, createdBy);
        internal void ReturnTempMaterial(string entryNo, string remarks) => _objDal.ReturnTempMaterial(entryNo, remarks);
        internal DataTable CreateSecurityIncident(SecurityIncident inc, int createdBy) => _objDal.CreateSecurityIncident(inc, createdBy);

        internal DataTable GetAllVisitors() => _objDal.GetAllVisitors();
        internal DataTable GetAllTempMaterials() => _objDal.GetAllTempMaterials();
        internal DataTable GetAllSecurityIncidents() => _objDal.GetAllSecurityIncidents();
    }
}
