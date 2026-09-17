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
                                InwardNo = reader["InwardNo"]?.ToString() ?? "",
                                GrossWeight = Convert.ToDecimal(reader["GrossWeight"]),
                                TareWeight = reader["TareWeight"] != DBNull.Value ? Convert.ToDecimal(reader["TareWeight"]) : null,
                                NetWeight = reader["NetWeight"] != DBNull.Value ? Convert.ToDecimal(reader["NetWeight"]) : null,
                                TargetOfficeId = reader["TargetOfficeId"] != DBNull.Value ? Convert.ToInt32(reader["TargetOfficeId"]) : null,
                                GateEntryTime = Convert.ToDateTime(reader["GateEntryTime"]),
                                GateExitTime = reader["GateExitTime"] != DBNull.Value ? Convert.ToDateTime(reader["GateExitTime"]) : null,
                                Status = reader["Status"].ToString() ?? "",
                                VehicleNumber = reader["VehicleNumber"].ToString() ?? "",
                                PartyName = reader["PartyName"].ToString() ?? "",
                                DriverName = reader["DriverName"].ToString() ?? "",
                                DriverMobile = reader["DriverMobile"]?.ToString() ?? "",
                                TotalBags = reader["TotalBags"] != DBNull.Value ? Convert.ToInt32(reader["TotalBags"]) : null,
                                WeighmentCharge = reader["WeighmentCharge"] != DBNull.Value ? Convert.ToDecimal(reader["WeighmentCharge"]) : 0,
                                InwardOutward = reader["InwardOutward"]?.ToString() ?? "Inward",
                                GateManName = reader["GateManName"]?.ToString() ?? "Weightman"
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
                    cmd.Parameters.AddWithValue("@InwardNo", (object?)entry.InwardNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleId", entry.VehicleId > 0 ? entry.VehicleId : 1);
                    cmd.Parameters.AddWithValue("@PartyId", entry.PartyId > 0 ? entry.PartyId : 1);
                    cmd.Parameters.AddWithValue("@DriverId", entry.DriverId > 0 ? entry.DriverId : 1);
                    cmd.Parameters.AddWithValue("@GrossWeight", entry.GrossWeight);
                    cmd.Parameters.AddWithValue("@TargetOfficeId", (object?)entry.TargetOfficeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", entry.CreatedBy > 0 ? entry.CreatedBy : 1);
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        rstNumber = result.ToString() ?? "";
                        entry.RSTNumber = rstNumber;
                        
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

        public List<dynamic> GetDriversWithMobile()
        {
            var list = new List<dynamic>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT PersonId, PersonName, ISNULL(MobileNumber, '') AS MobileNumber 
                    FROM p02_Person 
                    WHERE IsActive = 1 AND (PersonType = 'Driver' OR PersonType IS NULL OR TRIM(PersonType) = '')";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader["PersonName"].ToString() ?? "";
                            string mobile = reader["MobileNumber"].ToString() ?? "";
                            string displayName = string.IsNullOrWhiteSpace(mobile) ? name : $"{name} / {mobile}";
                            list.Add(new
                            {
                                DriverId = Convert.ToInt32(reader["PersonId"]),
                                DriverNameMobile = displayName
                            });
                        }
                    }
                }
            }
            if (list.Count == 0)
            {
                list.Add(new { DriverId = 1, DriverNameMobile = "Default Driver / 9876543210" });
            }
            return list;
        }

        public DataTable GetInwardDetails(string inwardNo)
        {
            var dt = new DataTable();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT h.InwardNo, h.PartyId, ISNULL(NULLIF(TRIM(h.PartyName),''), p.PersonName) AS PartyName, 
                           h.VehicleNo, v.VehicleId, h.DriverName, h.DriverMobile, d.PersonId AS DriverId
                    FROM t_InwardHeader h
                    LEFT JOIN p02_Person p ON h.PartyId = p.PersonId
                    LEFT JOIN m_Vehicle v ON UPPER(TRIM(h.VehicleNo)) = UPPER(TRIM(v.VehicleNumber))
                    LEFT JOIN p02_Person d ON LOWER(TRIM(h.DriverName)) = LOWER(TRIM(d.PersonName))
                    WHERE TRIM(h.InwardNo) = TRIM(@InwardNo)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@InwardNo", inwardNo ?? "");
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public dynamic GetLinkageByParty(int partyId)
        {
            int vehicleId = 0;
            int driverId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT TOP 1 VehicleId, DriverId 
                    FROM t_GateEntry 
                    WHERE PartyId = @PartyId 
                    ORDER BY GateEntryTime DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            vehicleId = rdr["VehicleId"] != DBNull.Value ? Convert.ToInt32(rdr["VehicleId"]) : 0;
                            driverId = rdr["DriverId"] != DBNull.Value ? Convert.ToInt32(rdr["DriverId"]) : 0;
                        }
                    }
                }
            }
            return new { vehicleId, driverId };
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

        public List<dynamic> GetVehiclesByParty(int partyId)
        {
            var list = new List<dynamic>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT DISTINCT v.VehicleId, v.VehicleNumber,
                           CASE WHEN EXISTS (
                               SELECT 1 FROM t_GateEntry ge WHERE ge.PartyId = @PartyId AND ge.VehicleId = v.VehicleId
                               UNION
                               SELECT 1 FROM t_InwardHeader ih WHERE ih.PartyId = @PartyId AND UPPER(TRIM(ih.VehicleNo)) = UPPER(TRIM(v.VehicleNumber))
                           ) THEN 1 ELSE 0 END AS IsLinked
                    FROM m_Vehicle v
                    WHERE v.IsActive = 1
                    ORDER BY IsLinked DESC, v.VehicleNumber ASC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new
                            {
                                VehicleId = Convert.ToInt32(rdr["VehicleId"]),
                                VehicleNumber = rdr["VehicleNumber"].ToString() ?? "",
                                IsLinked = Convert.ToInt32(rdr["IsLinked"]) == 1
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<dynamic> GetDriversByParty(int partyId)
        {
            var list = new List<dynamic>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT DISTINCT p.PersonId AS DriverId, 
                           CASE WHEN p.MobileNumber IS NULL OR TRIM(p.MobileNumber) = '' THEN p.PersonName ELSE p.PersonName + ' / ' + p.MobileNumber END AS DriverNameMobile,
                           CASE WHEN EXISTS (
                               SELECT 1 FROM t_GateEntry ge WHERE ge.PartyId = @PartyId AND ge.DriverId = p.PersonId
                               UNION
                               SELECT 1 FROM t_InwardHeader ih WHERE ih.PartyId = @PartyId AND LOWER(TRIM(ih.DriverName)) = LOWER(TRIM(p.PersonName))
                           ) THEN 1 ELSE 0 END AS IsLinked
                    FROM p02_Person p
                    WHERE p.IsActive = 1 AND (p.PersonType = 'Driver' OR p.PersonType IS NULL OR TRIM(p.PersonType) = '')
                    ORDER BY IsLinked DESC, DriverNameMobile ASC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new
                            {
                                DriverId = Convert.ToInt32(rdr["DriverId"]),
                                DriverNameMobile = rdr["DriverNameMobile"].ToString() ?? "",
                                IsLinked = Convert.ToInt32(rdr["IsLinked"]) == 1
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}
