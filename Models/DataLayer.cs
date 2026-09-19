using Microsoft.Data.SqlClient;
using System.Data;

namespace RiceMillProject.Models
{
    public class DataLayer
    {
        private readonly string _connectionString;

        public DataLayer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private DataTable ExecuteDataTable(string ProcdureName, SqlParameter[]? sp = null)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(ProcdureName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (sp != null)
                {
                    cmd.Parameters.AddRange(sp);
                }
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        private DataTable ExecuteDataTableWithTranscation(string ProcdureName, SqlParameter[] sp, SqlTransaction trans, SqlConnection con)
        {
            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(ProcdureName, con, trans))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (sp != null)
                {
                    cmd.Parameters.AddRange(sp);
                }
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        internal DataTable GetAlldhermkata()
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[2];
                sp[0] = new SqlParameter("@officeid", "0");
                sp[1] = new SqlParameter("@O04_childOfficeTypeId", "3");
                return ExecuteDataTable("Sp_GetAllDhermKataorLocations", sp);               
            }
            catch
            {
                return new DataTable();
            }
        }

        internal bool InsertDhermkataDetails(DhermKata obj)
        {
            bool flag = false;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed) con.Open();
                using SqlTransaction trans = con.BeginTransaction();
                try
                {                   
                    SqlParameter[] sp = new SqlParameter[3];
                    sp[0] = new SqlParameter("@officetype", "3");
                    sp[1] = new SqlParameter("@Location", "");
                    sp[2] = new SqlParameter("@OfficeName", obj.childofficeName);
                    DataTable dt = ExecuteDataTableWithTranscation("sp_InsertOffice", sp, trans, con);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        int OfficeId = Convert.ToInt32(dt.Rows[0]["OfficeId"]);
                        SqlParameter[] sp1 = new SqlParameter[4];
                        sp1[0] = new SqlParameter("@O05_ParentOfficeId", obj.Mainofficeid);
                        sp1[1] = new SqlParameter("@O05_childOfficeId", OfficeId);
                        sp1[2] = new SqlParameter("@O04_ParentOfficeTypeId", "1");
                        sp1[3] = new SqlParameter("@O04_childOfficeTypeId", "3");
                        DataTable dt1 = ExecuteDataTableWithTranscation("Sp_InsertRelationBetweenOffice", sp1, trans, con);
                        if (dt1 != null && dt1.Rows.Count > 0)
                        {
                            flag = true;
                            trans.Commit();
                        }
                        else
                        {
                            trans.Rollback();
                        }
                    }
                }
                catch
                {
                    trans.Rollback();
                }
            }
            return flag;
        }

        internal DataTable GetAllMainoffice()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllOffices");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetAllMeth()
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[] { new SqlParameter("@PersonType", "Meth") };
                return ExecuteDataTable("sp_GetPersonsByType", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetGatesByOffice(int officeId)
        {
            DataTable dt = new DataTable();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT GateId AS Value, GateName AS Text
                FROM dbo.m_GateMaster
                WHERE IsActive = 1 AND OfficeId = @OfficeId
                ORDER BY GateName", con);
            cmd.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        internal DataTable GetMethByOffice(int officeId)
        {
            DataTable dt = new DataTable();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT DISTINCT p.PersonId AS Value, p.PersonName AS Text
                FROM dbo.p02_Person p
                INNER JOIN dbo.P11_PersonDesignatation pd
                    ON pd.P02_PersonId = p.PersonId AND pd.IsActive = 1
                INNER JOIN dbo.O12_Designatation d
                    ON d.DesignationId = pd.O12_DesignationId AND d.IsActive = 1
                WHERE p.IsActive = 1
                  AND (d.O10_PostId = 6 OR p.PersonType = '6' OR LOWER(LTRIM(RTRIM(p.PersonType))) = 'meth')
                  AND d.O05_Office_Id = @OfficeId
                ORDER BY p.PersonName", con);
            cmd.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        internal DataTable GetEmployeePost()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllPosts");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetPost()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllPosts");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetWorkersByMeth(int methPersonId)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[] { new SqlParameter("@MethPersonId", methPersonId) };
                return ExecuteDataTable("sp_GetWorkersByMeth", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetActiveInwardEntriesForDropdown()
        {
            try
            {
                return ExecuteDataTable("sp_GetActiveInwardEntriesForDropdown");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable CreateGatemanInward(string vehicleNumber, string driverName, int gateManId, int? itemId, decimal? quantity, string? unit)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@VehicleNumber", vehicleNumber ?? (object)DBNull.Value),
                    new SqlParameter("@DriverName", driverName ?? (object)DBNull.Value),
                    new SqlParameter("@GateManId", gateManId),
                    new SqlParameter("@ItemId", itemId ?? (object)DBNull.Value),
                    new SqlParameter("@Quantity", quantity ?? (object)DBNull.Value),
                    new SqlParameter("@Unit", unit ?? (object)DBNull.Value),
                    new SqlParameter
                    {
                        ParameterName = "@InwardNo",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 50,
                        Direction = ParameterDirection.Output
                    }
                };
                return ExecuteDataTable("sp_CreateGatemanInward", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetChallanDetailsForOutward(string challanNo)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[] { new SqlParameter("@ChallanNo", challanNo) };
                return ExecuteDataTable("sp_GetChallanDetailsForOutward", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable ApproveOutwardChallan(string challanNo, int gateManId)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@ChallanNo", challanNo),
                    new SqlParameter("@GateManId", gateManId),
                    new SqlParameter
                    {
                        ParameterName = "@OutwardNo",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 50,
                        Direction = ParameterDirection.Output
                    }
                };
                return ExecuteDataTable("sp_ApproveOutwardChallan", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        // --- NEW PDF GATE MANAGEMENT MODULE DAL METHODS ---

        internal DataTable ManageGateMaster(string action, GateMaster? gate = null)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@Action", action),
                    new SqlParameter("@GateId", gate?.GateId ?? (object)DBNull.Value),
                    new SqlParameter("@GateCode", gate?.GateCode ?? (object)DBNull.Value),
                    new SqlParameter("@OfficeId", gate?.OfficeId > 0 ? gate.OfficeId : (object)DBNull.Value),
                    new SqlParameter("@GateName", gate?.GateName ?? (object)DBNull.Value),
                    new SqlParameter("@GateType", gate?.GateType ?? (object)DBNull.Value),
                    new SqlParameter("@LocationArea", gate?.LocationArea ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentId", gate?.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@InwardAllowed", gate?.InwardAllowed ?? true),
                    new SqlParameter("@OutwardAllowed", gate?.OutwardAllowed ?? true),
                    new SqlParameter("@IsActive", gate?.IsActive ?? true)
                };
                return ExecuteDataTable("sp_ManageGateMaster", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable ManageDepartmentMaster(string action, DepartmentMaster? dept = null)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@Action", action),
                    new SqlParameter("@DepartmentId", dept?.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentCode", dept?.DepartmentCode ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentName", dept?.DepartmentName ?? (object)DBNull.Value),
                    new SqlParameter("@OfficeId", dept?.OfficeId ?? (object)DBNull.Value),
                    new SqlParameter("@ResponsibleOfficerId", dept?.ResponsibleOfficerId ?? (object)DBNull.Value),
                    new SqlParameter("@IsActive", dept?.IsActive ?? true)
                };
                return ExecuteDataTable("sp_ManageDepartmentMaster", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable CreateVisitorEntry(VisitorRegister v, int createdBy)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@GateId", v.GateId),
                    new SqlParameter("@VisitorName", v.VisitorName),
                    new SqlParameter("@MobileNo", v.MobileNo),
                    new SqlParameter("@CompanyOrg", v.CompanyOrg ?? (object)DBNull.Value),
                    new SqlParameter("@PurposeOfVisit", v.PurposeOfVisit),
                    new SqlParameter("@PersonToMeetId", v.PersonToMeetId ?? (object)DBNull.Value),
                    new SqlParameter("@PersonToMeetName", v.PersonToMeetName ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentId", v.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@VisitorIDType", v.VisitorIDType ?? (object)DBNull.Value),
                    new SqlParameter("@IDReference", v.IDReference ?? (object)DBNull.Value),
                    new SqlParameter("@VehicleNo", v.VehicleNo ?? (object)DBNull.Value),
                    new SqlParameter("@Remarks", v.Remarks ?? (object)DBNull.Value),
                    new SqlParameter("@CreatedBy", createdBy)
                };
                return ExecuteDataTable("sp_CreateVisitorEntry", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal void ExitVisitor(string visitorNo)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[] { new SqlParameter("@VisitorNo", visitorNo) };
                ExecuteDataTable("sp_ExitVisitor", sp);
            }
            catch { }
        }

        internal DataTable CreateTempMaterialEntry(TempMaterialRegister item, int createdBy)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@GateId", item.GateId),
                    new SqlParameter("@OwnerVendor", item.OwnerVendor),
                    new SqlParameter("@VehicleNo", item.VehicleNo ?? (object)DBNull.Value),
                    new SqlParameter("@ItemCategory", item.ItemCategory),
                    new SqlParameter("@ItemDescription", item.ItemDescription),
                    new SqlParameter("@Quantity", item.Quantity),
                    new SqlParameter("@Unit", item.Unit),
                    new SqlParameter("@SerialAssetNo", item.SerialAssetNo ?? (object)DBNull.Value),
                    new SqlParameter("@Purpose", item.Purpose),
                    new SqlParameter("@DepartmentId", item.DepartmentId ?? (object)DBNull.Value),
                    new SqlParameter("@ExpectedReturnDate", item.ExpectedReturnDate ?? (object)DBNull.Value),
                    new SqlParameter("@Remarks", item.Remarks ?? (object)DBNull.Value),
                    new SqlParameter("@CreatedBy", createdBy)
                };
                return ExecuteDataTable("sp_CreateTempMaterialEntry", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal void ReturnTempMaterial(string entryNo, string remarks)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@EntryNo", entryNo),
                    new SqlParameter("@Remarks", remarks ?? (object)DBNull.Value)
                };
                ExecuteDataTable("sp_ReturnTempMaterial", sp);
            }
            catch { }
        }

        internal DataTable CreateSecurityIncident(SecurityIncident inc, int createdBy)
        {
            try
            {
                SqlParameter[] sp = new SqlParameter[]
                {
                    new SqlParameter("@GateId", inc.GateId),
                    new SqlParameter("@IncidentType", inc.IncidentType),
                    new SqlParameter("@RelatedEntryNo", inc.RelatedEntryNo ?? (object)DBNull.Value),
                    new SqlParameter("@Description", inc.Description),
                    new SqlParameter("@ActionTaken", inc.ActionTaken ?? (object)DBNull.Value),
                    new SqlParameter("@ReportedToPersonId", inc.ReportedToPersonId ?? (object)DBNull.Value),
                    new SqlParameter("@Severity", inc.Severity),
                    new SqlParameter("@CreatedBy", createdBy)
                };
                return ExecuteDataTable("sp_CreateSecurityIncident", sp);
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetAllVisitors()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllVisitors");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetAllTempMaterials()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllTempMaterials");
            }
            catch
            {
                return new DataTable();
            }
        }

        internal DataTable GetAllSecurityIncidents()
        {
            try
            {
                return ExecuteDataTable("sp_GetAllSecurityIncidents");
            }
            catch
            {
                return new DataTable();
            }
        }
    }
}
