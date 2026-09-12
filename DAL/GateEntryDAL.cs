using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class GateEntryDAL
    {
        private readonly string _connectionString;

        public GateEntryDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<GateEntry> GetAllGateEntries()
        {
            var entries = new List<GateEntry>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetAllGateEntries", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(new GateEntry
                            {
                                RSTNumber = reader["RSTNumber"].ToString() ?? "",
                                GrossWeight = Convert.ToDecimal(reader["GrossWeight"]),
                                TareWeight = reader["TareWeight"] != DBNull.Value ? Convert.ToDecimal(reader["TareWeight"]) : null,
                                NetWeight = reader["NetWeight"] != DBNull.Value ? Convert.ToDecimal(reader["NetWeight"]) : null,
                                TargetOfficeId = reader["TargetOfficeId"] != DBNull.Value ? Convert.ToInt32(reader["TargetOfficeId"]) : null,
                                GateEntryTime = Convert.ToDateTime(reader["GateEntryTime"]),
                                GateExitTime = reader["GateExitTime"] != DBNull.Value ? Convert.ToDateTime(reader["GateExitTime"]) : null,
                                Status = reader["Status"].ToString() ?? "",
                                VehicleNumber = reader["VehicleNumber"].ToString() ?? "",
                                PartyName = reader["PartyName"].ToString() ?? "",
                                DriverName = reader["DriverName"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            return entries;
        }

        public string CreateGateEntry(GateEntry entry)
        {
            string rstNumber = "";
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CreateGateEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RSTNumber", entry.RSTNumber);
                    cmd.Parameters.AddWithValue("@VehicleId", entry.VehicleId);
                    cmd.Parameters.AddWithValue("@PartyId", entry.PartyId);
                    cmd.Parameters.AddWithValue("@DriverId", entry.DriverId);
                    cmd.Parameters.AddWithValue("@GrossWeight", entry.GrossWeight);
                    cmd.Parameters.AddWithValue("@TargetOfficeId", entry.TargetOfficeId ?? (object)DBNull.Value);
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        rstNumber = result.ToString() ?? "";
                        
                        // Insert multiple locations
                        if (entry.TargetLocationIds != null && rstNumber != "")
                        {
                            foreach (int locId in entry.TargetLocationIds)
                            {
                                using (SqlCommand locCmd = new SqlCommand("sp_InsertGateEntryLocation", con))
                                {
                                    locCmd.CommandType = CommandType.StoredProcedure;
                                    locCmd.Parameters.AddWithValue("@RSTNumber", rstNumber);
                                    locCmd.Parameters.AddWithValue("@LocationId", locId);
                                    locCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            return rstNumber;
        }

        public bool CompleteGateExit(string rstNumber, decimal tareWeight)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CompleteGateExit", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RSTNumber", rstNumber);
                    cmd.Parameters.AddWithValue("@TareWeight", tareWeight);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }
    }
}
