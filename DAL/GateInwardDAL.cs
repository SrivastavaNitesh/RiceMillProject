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
                    cmd.Parameters.AddWithValue("@GateId", model.GateId);
                    cmd.Parameters.AddWithValue("@InwardTypeId", model.InwardTypeId);
                    cmd.Parameters.AddWithValue("@PartyId", model.PartyId);
                    cmd.Parameters.AddWithValue("@TransporterName", (object?)model.TransporterName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleNo", model.VehicleNo);
                    cmd.Parameters.AddWithValue("@VehicleTypeId", model.VehicleTypeId);
                    cmd.Parameters.AddWithValue("@DriverName", model.DriverName);
                    cmd.Parameters.AddWithValue("@DriverMobile", (object?)model.DriverMobile ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChallanNo", (object?)model.ChallanNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ApproxNoOfBags", model.ApproxNoOfBags);
                    cmd.Parameters.AddWithValue("@ApproxWeight", model.ApproxWeight);
                    cmd.Parameters.AddWithValue("@PurposeRemarks", (object?)model.PurposeRemarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", 1);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }
    }
}
