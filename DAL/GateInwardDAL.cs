using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class GateInwardDAL
    {
        private readonly string _connectionString;

        public GateInwardDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<InwardTypeMaster> GetInwardTypes()
        {
            var list = new List<InwardTypeMaster>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT InwardTypeId, InwardTypeName, RSTRequired, IsActive FROM m_InwardTypeMaster WHERE IsActive = 1", con))
                {
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new InwardTypeMaster
                            {
                                InwardTypeId = Convert.ToInt32(rdr["InwardTypeId"]),
                                InwardTypeName = rdr["InwardTypeName"].ToString() ?? "",
                                RSTRequired = Convert.ToBoolean(rdr["RSTRequired"]),
                                IsActive = Convert.ToBoolean(rdr["IsActive"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<VehicleTypeMaster> GetVehicleTypes()
        {
            var list = new List<VehicleTypeMaster>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT VehicleTypeId, VehicleTypeName, IsActive FROM m_VehicleTypeMaster WHERE IsActive = 1", con))
                {
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new VehicleTypeMaster
                            {
                                VehicleTypeId = Convert.ToInt32(rdr["VehicleTypeId"]),
                                VehicleTypeName = rdr["VehicleTypeName"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(rdr["IsActive"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<InwardHeader> GetAllInwardEntries()
        {
            var list = new List<InwardHeader>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageGateInwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT_ALL");
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new InwardHeader
                            {
                                InwardId = Convert.ToInt32(rdr["InwardId"]),
                                GateEntryNo = rdr["GateEntryNo"]?.ToString(),
                                InwardNo = rdr["InwardNo"]?.ToString(),
                                InwardDate = Convert.ToDateTime(rdr["InwardDate"]),
                                InwardTime = rdr["InwardTime"]?.ToString(),
                                GateName = rdr["GateName"]?.ToString(),
                                InwardTypeName = rdr["InwardTypeName"]?.ToString(),
                                RSTRequired = Convert.ToBoolean(rdr["RSTRequired"]),
                                PartyName = rdr["PartyName"]?.ToString(),
                                TransporterName = rdr["TransporterName"]?.ToString(),
                                VehicleNo = rdr["VehicleNo"]?.ToString() ?? "",
                                VehicleTypeName = rdr["VehicleTypeName"]?.ToString(),
                                DriverName = rdr["DriverName"]?.ToString() ?? "",
                                DriverMobile = rdr["DriverMobile"]?.ToString(),
                                ChallanNo = rdr["ChallanNo"]?.ToString(),
                                ApproxNoOfBags = rdr["ApproxNoOfBags"] != DBNull.Value ? Convert.ToInt32(rdr["ApproxNoOfBags"]) : 0,
                                ApproxWeight = rdr["ApproxWeight"] != DBNull.Value ? Convert.ToDecimal(rdr["ApproxWeight"]) : 0,
                                PurposeRemarks = rdr["PurposeRemarks"]?.ToString(),
                                GateInDateTime = Convert.ToDateTime(rdr["GateInDateTime"]),
                                GateManName = rdr["GateManName"]?.ToString() ?? "Gateman",
                                StatusName = rdr["StatusName"]?.ToString() ?? "Pending",
                                IsRSTGenerated = Convert.ToBoolean(rdr["IsRSTGenerated"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool SaveInwardEntry(InwardHeader model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageGateInwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "INSERT");
                    cmd.Parameters.AddWithValue("@GateId", model.GateId > 0 ? model.GateId : 1);
                    cmd.Parameters.AddWithValue("@InwardTypeId", model.InwardTypeId > 0 ? model.InwardTypeId : 1);
                    cmd.Parameters.AddWithValue("@PartyId", (object?)model.PartyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PartyName", (object?)model.PartyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransporterName", (object?)model.TransporterName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleNo", model.VehicleNo ?? "");
                    cmd.Parameters.AddWithValue("@VehicleTypeId", model.VehicleTypeId > 0 ? model.VehicleTypeId : 1);
                    cmd.Parameters.AddWithValue("@DriverName", model.DriverName ?? "");
                    cmd.Parameters.AddWithValue("@DriverMobile", (object?)model.DriverMobile ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChallanNo", (object?)model.ChallanNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ApproxNoOfBags", model.ApproxNoOfBags);
                    cmd.Parameters.AddWithValue("@ApproxWeight", model.ApproxWeight);
                    cmd.Parameters.AddWithValue("@PurposeRemarks", (object?)model.PurposeRemarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy > 0 ? model.CreatedBy : 1);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            model.InwardId = Convert.ToInt32(rdr["InwardId"]);
                            model.InwardNo = rdr["InwardNo"]?.ToString();
                            model.GateEntryNo = rdr["GateEntryNo"]?.ToString();
                        }
                    }
                    return true;
                }
            }
        }

        public List<dynamic> GetGatemenList()
        {
            var list = new List<dynamic>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT u.UserId, u.Username AS GatemanName 
                    FROM sa05_user u 
                    LEFT JOIN sa10_usertype t ON u.sa10_usertypeid = t.UserTypeId 
                    WHERE t.UserTypeName LIKE '%Gate%' OR u.sa10_usertypeid = 2 OR u.UserId = 2";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new
                            {
                                UserId = Convert.ToInt32(rdr["UserId"]),
                                GatemanName = rdr["GatemanName"].ToString() ?? "Gateman " + rdr["UserId"]
                            });
                        }
                    }
                }
            }
            if (list.Count == 0)
            {
                list.Add(new { UserId = 2, GatemanName = "Gateman (System)" });
            }
            return list;
        }

        public bool IsDriverMobileDuplicate(string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber)) return false;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT COUNT(1) FROM t_InwardHeader WHERE TRIM(DriverMobile) = TRIM(@Mobile)
                    UNION ALL
                    SELECT COUNT(1) FROM p02_Person WHERE TRIM(MobileNumber) = TRIM(@Mobile) AND PersonType = 'Driver'";
                using (SqlCommand cmd = new SqlCommand("SELECT SUM(c) FROM (" + query + ") AS t(c)", con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobileNumber.Trim());
                    con.Open();
                    object? res = cmd.ExecuteScalar();
                    int count = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
                    return count > 0;
                }
            }
        }
    }
}
