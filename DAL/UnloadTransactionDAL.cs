using System;
using System.Collections.Generic;
using System.Data;
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
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<UnloadTransaction> GetAllUnloading()
        {
            var list = new List<UnloadTransaction>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT u.*, 
       s.PersonName as SupervisorName,
       m.PersonName as MethName,
       b.BagTypeName
FROM t_UnloadTransaction u
LEFT JOIN p02_Person s ON u.SupervisorId = s.PersonId
LEFT JOIN p02_Person m ON u.MethId = m.PersonId
LEFT JOIN m_BagType b ON u.BagTypeId = b.BagTypeId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new UnloadTransaction
                            {
                                UnloadId = Convert.ToInt32(reader["UnloadId"]),
                                RSTNumber = reader["RSTNumber"].ToString() ?? "",
                                SupervisorId = reader["SupervisorId"] != DBNull.Value ? Convert.ToInt32(reader["SupervisorId"]) : 0,
                                GateManId = reader["GateManId"] != DBNull.Value ? Convert.ToInt32(reader["GateManId"]) : 0,
                                MethId = reader["MethId"] != DBNull.Value ? Convert.ToInt32(reader["MethId"]) : 0,
                                BagTypeId = reader["BagTypeId"] != DBNull.Value ? Convert.ToInt32(reader["BagTypeId"]) : null,
                                NumberOfBags = reader["NumberOfBags"] != DBNull.Value ? Convert.ToInt32(reader["NumberOfBags"]) : null,
                                TotalBagDeductionGrams = reader["TotalBagDeductionGrams"] != DBNull.Value ? Convert.ToDecimal(reader["TotalBagDeductionGrams"]) : null,
                                UnloadTime = reader["UnloadTime"] != DBNull.Value ? Convert.ToDateTime(reader["UnloadTime"]) : null,
                                Status = reader["Status"].ToString() ?? "Assigned",
                                SupervisorName = reader["SupervisorName"].ToString(),
                                MethName = reader["MethName"].ToString(),
                                BagTypeName = reader["BagTypeName"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<Person> GetUnloadingPeople()
        {
            var people = new List<Person>();
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UnloadPeople", con) { CommandType = CommandType.StoredProcedure };
            con.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) people.Add(new Person
            {
                PersonId = Convert.ToInt32(reader["PersonId"]), PersonName = reader["PersonName"].ToString() ?? "",
                PersonType = reader["RoleName"].ToString() ?? ""
            });
            return people;
        }

        public void CompleteWithItems(UnloadTransaction unload, int locationId, string shift, int[] workerIds)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CompleteUnloadWithItems", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@UnloadId", SqlDbType.Int).Value = unload.UnloadId;
            cmd.Parameters.Add("@GateManId", SqlDbType.Int).Value = unload.GateManId;
            cmd.Parameters.Add("@BagTypeId", SqlDbType.Int).Value = unload.BagTypeId!.Value;
            cmd.Parameters.Add("@NumberOfBags", SqlDbType.Int).Value = unload.NumberOfBags!.Value;
            cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
            cmd.Parameters.Add("@Shift", SqlDbType.VarChar, 10).Value = shift;
            cmd.Parameters.Add("@ItemIds", SqlDbType.NVarChar, -1).Value = System.Text.Json.JsonSerializer.Serialize(unload.SelectedItemIds);
            cmd.Parameters.Add("@WorkerIds", SqlDbType.NVarChar, -1).Value = System.Text.Json.JsonSerializer.Serialize(workerIds);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public int AssignUnloading(string rstNumber, int supervisorId, int methId)
        {
            int unloadId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_AssignUnloading", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RSTNumber", rstNumber);
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                    cmd.Parameters.AddWithValue("@MethId", methId);
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        unloadId = Convert.ToInt32(result);
                    }
                }
            }
            return unloadId;
        }

        public bool SubmitUnloading(int unloadId, int gateManId, int bagTypeId, int numberOfBags)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_SubmitUnloading", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UnloadId", unloadId);
                    cmd.Parameters.AddWithValue("@GateManId", gateManId);
                    cmd.Parameters.AddWithValue("@BagTypeId", bagTypeId);
                    cmd.Parameters.AddWithValue("@NumberOfBags", numberOfBags);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }

        public bool VerifyUnloading(int unloadId, decimal totalBagDeductionGrams)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_VerifyUnloading", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UnloadId", unloadId);
                    cmd.Parameters.AddWithValue("@TotalBagDeductionGrams", totalBagDeductionGrams);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }

        public bool SaveWorkerAllocation(int unloadId, int workerId, decimal palledariAmount)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_SaveWorkerAllocation", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UnloadId", unloadId);
                    cmd.Parameters.AddWithValue("@WorkerId", workerId);
                    cmd.Parameters.AddWithValue("@PalledariAmount", palledariAmount);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }

        public void SaveUnloadLocation(int unloadId, int locationId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertUnloadLocation", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UnloadId", unloadId);
                    cmd.Parameters.AddWithValue("@LocationId", locationId);
                    
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
