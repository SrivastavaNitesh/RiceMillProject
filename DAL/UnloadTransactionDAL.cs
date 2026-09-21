using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class UnloadTransactionDAL
    {
        private readonly string _connectionString;

        public UnloadTransactionDAL(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private static bool HasColumn(
            SqlDataReader reader,
            string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i)
                    .Equals(
                        columnName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        // ============================================================
        // GET ALL UNLOADING
        // ============================================================

        public List<UnloadTransaction> GetAllUnloading()
        {
            var list = new List<UnloadTransaction>();

            using SqlConnection con =
                new SqlConnection(_connectionString);

            string query = @"
SELECT
    u.*,
    s.PersonName AS SupervisorName,
    m.PersonName AS MethName,
    b.BagTypeName,
    i.ItemName,
    l.LocationName
FROM dbo.t_UnloadTransaction u

LEFT JOIN dbo.P02_Person s
    ON u.SupervisorId = s.PersonId

LEFT JOIN dbo.P02_Person m
    ON u.MethId = m.PersonId

LEFT JOIN dbo.m_BagType b
    ON u.BagTypeId = b.BagTypeId

LEFT JOIN dbo.m_Item i
    ON u.ItemId = i.ItemId

LEFT JOIN dbo.m_LocationMaster l
    ON u.LocationId = l.LocationId

ORDER BY u.UnloadId DESC;";


            using SqlCommand cmd =
                new SqlCommand(query, con);

            con.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(
                    new UnloadTransaction
                    {
                        UnloadId =
                            Convert.ToInt32(
                                reader["UnloadId"]),

                        RSTNumber =
                            reader["RSTNumber"]
                                ?.ToString() ?? "",

                        LocationId =
                            HasColumn(reader, "LocationId") &&
                            reader["LocationId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["LocationId"])
                                : null,

                        SupervisorId =
                            reader["SupervisorId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["SupervisorId"])
                                : 0,

                        GateManId =
                            HasColumn(reader, "GateManId") &&
                            reader["GateManId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["GateManId"])
                                : 0,

                        MethId =
                            reader["MethId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["MethId"])
                                : 0,

                        ItemId =
                            HasColumn(reader, "ItemId") &&
                            reader["ItemId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["ItemId"])
                                : null,

                        BagTypeId =
                            reader["BagTypeId"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["BagTypeId"])
                                : null,

                        NumberOfBags =
                            reader["NumberOfBags"] != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["NumberOfBags"])
                                : null,

                        TotalBagDeductionGrams =
                            reader["TotalBagDeductionGrams"] != DBNull.Value
                                ? Convert.ToDecimal(
                                    reader["TotalBagDeductionGrams"])
                                : null,

                        UnloadTime =
                            reader["UnloadTime"] != DBNull.Value
                                ? Convert.ToDateTime(
                                    reader["UnloadTime"])
                                : null,

                        Status =
                            HasColumn(reader, "Status") &&
                            reader["Status"] != DBNull.Value
                                ? reader["Status"]
                                    .ToString() ?? "Assigned"
                                : "Assigned",

                        SupervisorName =
                            reader["SupervisorName"] != DBNull.Value
                                ? reader["SupervisorName"]
                                    .ToString()
                                : "",

                        MethName =
                            reader["MethName"] != DBNull.Value
                                ? reader["MethName"]
                                    .ToString()
                                : "",

                        BagTypeName =
                            reader["BagTypeName"] != DBNull.Value
                                ? reader["BagTypeName"]
                                    .ToString()
                                : "",

                        ItemName =
                            HasColumn(reader, "ItemName") &&
                            reader["ItemName"] != DBNull.Value
                                ? reader["ItemName"]
                                    .ToString()
                                : "Paddy Variety",

                        LocationName =
                            HasColumn(reader, "LocationName") &&
                            reader["LocationName"] != DBNull.Value
                                ? reader["LocationName"]
                                    .ToString()
                                : ""
                    });
            }

            return list;
        }


        // ============================================================
        // PEOPLE USED IN LEGACY UNLOAD SCREEN
        // ============================================================

        public List<Person> GetUnloadingPeople()
        {
            var people = new List<Person>();

            using var con =
                new SqlConnection(_connectionString);

            using var cmd =
                new SqlCommand(
                    "sp_UnloadPeople",
                    con)
                {
                    CommandType =
                        CommandType.StoredProcedure
                };

            con.Open();

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                people.Add(
                    new Person
                    {
                        PersonId =
                            Convert.ToInt32(
                                reader["PersonId"]),

                        PersonName =
                            reader["PersonName"]
                                ?.ToString() ?? "",

                        PersonType =
                            reader["RoleName"]
                                ?.ToString() ?? ""
                    });
            }

            return people;
        }


        // ============================================================
        // EXISTING COMPLETE WITH ITEMS
        // ============================================================

        public void CompleteWithItems(
            UnloadTransaction unload,
            int locationId,
            string shift,
            int[] workerIds)
        {
            using var con =
                new SqlConnection(_connectionString);

            using var cmd =
                new SqlCommand(
                    "sp_CompleteUnloadWithItems",
                    con)
                {
                    CommandType =
                        CommandType.StoredProcedure
                };

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unload.UnloadId;

            cmd.Parameters.Add(
                "@GateManId",
                SqlDbType.Int
            ).Value = unload.GateManId;

            cmd.Parameters.Add(
                "@BagTypeId",
                SqlDbType.Int
            ).Value = unload.BagTypeId!.Value;

            cmd.Parameters.Add(
                "@NumberOfBags",
                SqlDbType.Int
            ).Value = unload.NumberOfBags!.Value;

            cmd.Parameters.Add(
                "@LocationId",
                SqlDbType.Int
            ).Value = locationId;

            cmd.Parameters.Add(
                "@Shift",
                SqlDbType.VarChar,
                10
            ).Value = shift;

            cmd.Parameters.Add(
                "@ItemIds",
                SqlDbType.NVarChar,
                -1
            ).Value =
                JsonSerializer.Serialize(
                    unload.SelectedItemIds);

            cmd.Parameters.Add(
                "@WorkerIds",
                SqlDbType.NVarChar,
                -1
            ).Value =
                JsonSerializer.Serialize(
                    workerIds);

            con.Open();

            cmd.ExecuteNonQuery();
        }


        // ============================================================
        // ASSIGN UNLOADING
        // Supervisor assigns Meth + Location
        // ============================================================

        public int AssignUnloading(string rstNumber,int supervisorId,int methId,int? itemId,int locationId)
        {
            int unloadId = 0;

            using SqlConnection con =new SqlConnection(_connectionString);

            using SqlCommand cmd =new SqlCommand("sp_AssignUnloading",con);

            cmd.CommandType =CommandType.StoredProcedure;

            cmd.Parameters.Add("@RSTNumber",
                SqlDbType.NVarChar,
                100
            ).Value = rstNumber;

            cmd.Parameters.Add(
                "@SupervisorId",
                SqlDbType.Int
            ).Value = supervisorId;

            cmd.Parameters.Add(
                "@MethId",
                SqlDbType.Int
            ).Value = methId;

            cmd.Parameters.Add(
                "@ItemId",
                SqlDbType.Int
            ).Value =
                (object?)itemId
                ?? DBNull.Value;

            cmd.Parameters.Add(
                "@LocationId",
                SqlDbType.Int
            ).Value = locationId;

            con.Open();

            object? result =
                cmd.ExecuteScalar();

            if (result != null &&
                result != DBNull.Value)
            {
                unloadId =
                    Convert.ToInt32(result);
            }

            return unloadId;
        }


        // ============================================================
        // OLD SUBMIT UNLOADING
        // ============================================================

        public bool SubmitUnloading(
            int unloadId,
            int gateManId,
            int bagTypeId,
            int numberOfBags)
        {
            using SqlConnection con =
                new SqlConnection(
                    _connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_SubmitUnloading",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;

            cmd.Parameters.Add(
                "@GateManId",
                SqlDbType.Int
            ).Value = gateManId;

            cmd.Parameters.Add(
                "@BagTypeId",
                SqlDbType.Int
            ).Value = bagTypeId;

            cmd.Parameters.Add(
                "@NumberOfBags",
                SqlDbType.Int
            ).Value = numberOfBags;

            con.Open();

            int rowsAffected =
                cmd.ExecuteNonQuery();

            return rowsAffected > 0;
        }


        // ============================================================
        // VERIFY
        // ============================================================

        public bool VerifyUnloading(
            int unloadId,
            decimal totalBagDeductionGrams)
        {
            using SqlConnection con =
                new SqlConnection(
                    _connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_VerifyUnloading",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;

            cmd.Parameters.Add(
                "@TotalBagDeductionGrams",
                SqlDbType.Decimal
            ).Value =
                totalBagDeductionGrams;

            con.Open();

            int rowsAffected =
                cmd.ExecuteNonQuery();

            return rowsAffected > 0;
        }


        // ============================================================
        // OLD WORKER ALLOCATION
        // ============================================================

        public bool SaveWorkerAllocation(
            int unloadId,
            int workerId,
            decimal palledariAmount)
        {
            using SqlConnection con =
                new SqlConnection(
                    _connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_SaveWorkerAllocation",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;

            cmd.Parameters.Add(
                "@WorkerId",
                SqlDbType.Int
            ).Value = workerId;

            cmd.Parameters.Add(
                "@PalledariAmount",
                SqlDbType.Decimal
            ).Value = palledariAmount;

            con.Open();

            int rowsAffected =
                cmd.ExecuteNonQuery();

            return rowsAffected > 0;
        }


        // ============================================================
        // SAVE UNLOAD LOCATION
        // ============================================================

        public void SaveUnloadLocation(
            int unloadId,
            int locationId)
        {
            using SqlConnection con =
                new SqlConnection(
                    _connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_InsertUnloadLocation",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;

            cmd.Parameters.Add(
                "@LocationId",
                SqlDbType.Int
            ).Value = locationId;

            con.Open();

            cmd.ExecuteNonQuery();
        }


        // ============================================================
        // METH DASHBOARD
        // Logged in Meth ko sirf uske assignments
        // ============================================================

        public List<UnloadTransaction>
            GetMethAssignments(
                int methPersonId)
        {
            var list =
                new List<UnloadTransaction>();

            using var con =
                new SqlConnection(
                    _connectionString);

            using var cmd =
                new SqlCommand(
                    "sp_GetMethAssignments",
                    con)
                {
                    CommandType =
                        CommandType.StoredProcedure
                };

            cmd.Parameters.Add(
                "@MethPersonId",
                SqlDbType.Int
            ).Value = methPersonId;

            con.Open();

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(
                    new UnloadTransaction
                    {
                        UnloadId =
                            Convert.ToInt32(
                                reader["UnloadId"]),

                        RSTNumber =
                            reader["RSTNumber"]
                                ?.ToString() ?? "",

                        LocationId =
                            reader["LocationId"] ==
                            DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    reader["LocationId"]),

                        SupervisorId =
                            reader["SupervisorId"] ==
                            DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    reader["SupervisorId"]),

                        MethId =
                            reader["MethId"] ==
                            DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    reader["MethId"]),

                        ItemId =
                            reader["ItemId"] ==
                            DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    reader["ItemId"]),

                        NumberOfBags =
                            reader["NumberOfBags"] ==
                            DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    reader["NumberOfBags"]),

                        UnloadTime =
                            reader["UnloadTime"] ==
                            DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    reader["UnloadTime"]),

                        Status =
                            reader["Status"]
                                ?.ToString() ?? "",

                        SupervisorName =
                            reader["SupervisorName"]
                                ?.ToString() ?? "",

                        MethName =
                            reader["MethName"]
                                ?.ToString() ?? "",

                        ItemName =
                            reader["ItemName"]
                                ?.ToString() ?? "",

                        LocationName =
                            reader["LocationName"]
                                ?.ToString() ?? "",

                        VehicleNumber =
                            reader["VehicleNumber"]
                                ?.ToString() ?? "",

                        PartyName =
                            reader["PartyName"]
                                ?.ToString() ?? "",

                        OfficeName =
                            reader["OfficeName"]
                                ?.ToString() ?? "",

                        GrossWeight =
                            reader["GrossWeight"] ==
                            DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    reader["GrossWeight"]),

                        TareWeight =
                            reader["TareWeight"] ==
                            DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    reader["TareWeight"]),

                        NetWeight =
                            reader["NetWeight"] ==
                            DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    reader["NetWeight"])
                    });
            }

            return list;
        }


        // ============================================================
        // METH KE UNDER WORKERS
        // ============================================================

        public List<Person>
            GetWorkersByMeth(
                int methPersonId)
        {
            var workers =
                new List<Person>();

            using var con =
                new SqlConnection(
                    _connectionString);

            using var cmd =
                new SqlCommand(
                    "sp_GetWorkersByMeth",
                    con)
                {
                    CommandType =
                        CommandType.StoredProcedure
                };

            cmd.Parameters.Add(
                "@MethPersonId",
                SqlDbType.Int
            ).Value = methPersonId;

            con.Open();

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                workers.Add(
                    new Person
                    {
                        PersonId =
                            Convert.ToInt32(
                                reader["PersonId"]),

                        PersonName =
                            reader["PersonName"]
                                ?.ToString() ?? "",

                        PersonType =
                            "Worker"
                    });
            }

            return workers;
        }


        // ============================================================
        // METH EXECUTE UNLOAD SUBMIT
        // Worker wise Load/Unload + Bag Type + Bags + Charge
        // ============================================================

        public void CompleteMethUnload(
            int unloadId,
            int methPersonId,
            List<WorkerAllocation> workerRows)
        {
            var validRows =
                workerRows
                    .Where(x =>
                        x.BagCount > 0)
                    .Select(x => new
                    {
                        WorkerId =
                            x.WorkerId,

                        WorkType =
                            x.WorkType,

                        BagTypeId =
                            x.BagTypeId,

                        BagCount =
                            x.BagCount,

                        PerBagCharge =
                            x.PerBagCharge
                    })
                    .ToList();


            string workerJson =
                JsonSerializer.Serialize(
                    validRows);


            using var con =
                new SqlConnection(
                    _connectionString);


            using var cmd =
                new SqlCommand(
                    "sp_MethCompleteUnload",
                    con)
                {
                    CommandType =
                        CommandType.StoredProcedure
                };


            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;


            cmd.Parameters.Add(
                "@MethPersonId",
                SqlDbType.Int
            ).Value = methPersonId;


            cmd.Parameters.Add(
                "@WorkerRows",
                SqlDbType.NVarChar,
                -1
            ).Value = workerJson;


            con.Open();

            cmd.ExecuteNonQuery();
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
            string detailJson =
                JsonSerializer.Serialize(categoryRows);

            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_SaveSupervisorUnloadAction",
                    con
                );

            cmd.CommandType =
                CommandType.StoredProcedure;


            // =====================================================
            // PARAMETERS
            // =====================================================

            cmd.Parameters.Add(
                "@UnloadId",
                SqlDbType.Int
            ).Value = unloadId;


            cmd.Parameters.Add(
                "@SupervisorId",
                SqlDbType.Int
            ).Value = supervisorId;


            cmd.Parameters.Add(
                "@StackPP",
                SqlDbType.Int
            ).Value = stackPP;


            cmd.Parameters.Add(
                "@StackJute",
                SqlDbType.Int
            ).Value = stackJute;


            cmd.Parameters.Add(
                "@HaudiPP",
                SqlDbType.Int
            ).Value = haudiPP;


            cmd.Parameters.Add(
                "@HaudiJute",
                SqlDbType.Int
            ).Value = haudiJute;


            cmd.Parameters.Add(
                "@DetailJson",
                SqlDbType.NVarChar,
                -1
            ).Value = detailJson;


            // =====================================================
            // EXECUTE
            // =====================================================

            con.Open();

            object? result =
                cmd.ExecuteScalar();


            if (
                result == null
                ||
                result == DBNull.Value
            )
            {
                throw new InvalidOperationException(
                    "Supervisor unloading details could not be saved."
                );
            }


            return Convert.ToInt32(result);
        }
        public List<UnloadTransaction> GetSupervisorMethWorkRegister(
    int supervisorId)
        {
            var list =
                new List<UnloadTransaction>();

            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_GetSupervisorMethWorkRegister",
                    con
                );

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@SupervisorId",
                SqlDbType.Int
            ).Value =
                supervisorId;

            con.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(
                    new UnloadTransaction
                    {
                        UnloadId =
                            Convert.ToInt32(
                                reader["UnloadId"]
                            ),

                        RSTNumber =
                            reader["RSTNumber"]
                                ?.ToString()
                            ?? "",

                        SupervisorId =
                            reader["SupervisorId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["SupervisorId"]
                                )
                                : 0,

                        MethId =
                            reader["MethId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["MethId"]
                                )
                                : 0,

                        LocationId =
                            reader["LocationId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["LocationId"]
                                )
                                : 0,

                        ItemId =
                            reader["ItemId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["ItemId"]
                                )
                                : 0,

                        NumberOfBags =
                            reader["NumberOfBags"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["NumberOfBags"]
                                )
                                : null,

                        Status =
                            reader["Status"]
                                ?.ToString()
                            ?? "",

                        MethName =
                            reader["MethName"]
                                ?.ToString()
                            ?? "",

                        LocationName =
                            reader["LocationName"]
                                ?.ToString()
                            ?? "",

                        VehicleNumber =
                            reader["VehicleNumber"]
                                ?.ToString()
                            ?? "",

                        PartyName =
                            reader["PartyName"]
                                ?.ToString()
                            ?? "",

                        ItemName =
                            reader["ItemName"]
                                ?.ToString()
                            ?? "",

                        IsSupervisorActionCompleted =
                            reader["IsSupervisorActionCompleted"]
                                != DBNull.Value
                                &&
                                Convert.ToBoolean(
                                    reader["IsSupervisorActionCompleted"]
                                )
                    }
                );
            }

            return list;
        }
    }
}