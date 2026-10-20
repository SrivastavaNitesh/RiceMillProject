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
                        var hasCreatedBy = HasColumn(rdr, "CreatedBy");
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
                                CreatedBy = hasCreatedBy && rdr["CreatedBy"] != DBNull.Value ? Convert.ToInt32(rdr["CreatedBy"]) : 0,
                                GateManName = rdr["GateManName"]?.ToString() ?? "Gateman",
                                StatusName = rdr["StatusName"]?.ToString() ?? "Pending",
                                IsRSTGenerated = Convert.ToBoolean(rdr["IsRSTGenerated"])
                            });
                        }
                    }
                }
            }
            using (SqlConnection statusCon = new SqlConnection(_connectionString))
            {
                statusCon.Open();
                using var statusCmd = new SqlCommand(@"WITH ranked AS (
                    SELECT g.InwardNo, g.RSTNumber, g.GrossWeight, g.Status,
                           COALESCE(s.StatusName, g.Status) AS StatusName,
                           ROW_NUMBER() OVER (PARTITION BY g.InwardNo ORDER BY
                               CASE WHEN g.Status IN ('UnloadingAssigned','Unloaded','WaitingForTare') THEN 0 ELSE 1 END,
                               g.GateEntryTime DESC) AS rn
                    FROM dbo.t_GateEntry g
                    LEFT JOIN dbo.m_StatusMaster s ON s.StatusId = g.StatusId
                    WHERE g.InwardNo IS NOT NULL)
                    SELECT InwardNo, RSTNumber, GrossWeight, Status, StatusName FROM ranked WHERE rn=1", statusCon);
                using var statusReader = statusCmd.ExecuteReader();
                var statusByInward = new Dictionary<string, (string? Rst, string? Status, string? StatusName, decimal? Gross)>();
                while (statusReader.Read())
                {
                    statusByInward[statusReader["InwardNo"]?.ToString() ?? ""] = (
                        statusReader["RSTNumber"]?.ToString(), statusReader["Status"]?.ToString(), statusReader["StatusName"]?.ToString(),
                        statusReader["GrossWeight"] == DBNull.Value ? null : Convert.ToDecimal(statusReader["GrossWeight"]));
                }
                foreach (var inward in list)
                {
                    if (statusByInward.TryGetValue(inward.InwardNo ?? "", out var status))
                    {
                        inward.RstStatus = status.Status;
                        inward.RstStatusName = status.StatusName;
                        inward.RSTNumber = status.Rst;
                        inward.RSTGrossWeight = status.Gross;
                    }
                }
            }
            return list;
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public InwardHeader? GetInwardPrint(int inwardId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
SELECT TOP (1)
    h.InwardId,h.GateEntryNo,h.InwardNo,h.InwardDate,h.InwardTime,h.GateId,
    ISNULL(g.GateName,'') GateName,h.InwardTypeId,ISNULL(it.InwardTypeName,'') InwardTypeName,
    ISNULL(it.RSTRequired,0) RSTRequired,h.PartyId,
    COALESCE(NULLIF(h.PartyName,''),p.PersonName,'') PartyName,ISNULL(p.MobileNumber,'') PartyMobile,
    ISNULL(h.TransporterName,'') TransporterName,ISNULL(h.VehicleNo,'') VehicleNo,h.VehicleTypeId,
    ISNULL(vt.VehicleTypeName,'') VehicleTypeName,ISNULL(h.DriverName,'') DriverName,
    ISNULL(h.DriverMobile,'') DriverMobile,ISNULL(h.ChallanNo,'') ChallanNo,
    ISNULL(h.ApproxNoOfBags,0) ApproxNoOfBags,ISNULL(h.ApproxWeight,0) ApproxWeight,
    ISNULL(h.PurposeRemarks,'') PurposeRemarks,h.GateInDateTime,h.CreatedBy,
    ISNULL(u.Username,'Gateman') GateManName,ISNULL(s.StatusName,'Pending') StatusName,
    ISNULL(h.IsRSTGenerated,0) IsRSTGenerated
FROM dbo.t_InwardHeader h
LEFT JOIN dbo.m_GateMaster g ON g.GateId=h.GateId
LEFT JOIN dbo.m_InwardTypeMaster it ON it.InwardTypeId=h.InwardTypeId
LEFT JOIN dbo.P02_Person p ON p.PersonId=h.PartyId
LEFT JOIN dbo.m_VehicleTypeMaster vt ON vt.VehicleTypeId=h.VehicleTypeId
LEFT JOIN dbo.sa05_user u ON u.UserId=h.CreatedBy
LEFT JOIN dbo.m_StatusMaster s ON s.StatusId=h.StatusId
WHERE h.InwardId=@InwardId;", con);
            cmd.Parameters.Add("@InwardId", SqlDbType.Int).Value = inwardId;
            con.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new InwardHeader
            {
                InwardId = Convert.ToInt32(reader["InwardId"]), GateEntryNo = reader["GateEntryNo"]?.ToString(),
                InwardNo = reader["InwardNo"]?.ToString(), InwardDate = Convert.ToDateTime(reader["InwardDate"]),
                InwardTime = reader["InwardTime"]?.ToString(), GateId = Convert.ToInt32(reader["GateId"]),
                GateName = reader["GateName"]?.ToString(), InwardTypeId = Convert.ToInt32(reader["InwardTypeId"]),
                InwardTypeName = reader["InwardTypeName"]?.ToString(), RSTRequired = Convert.ToBoolean(reader["RSTRequired"]),
                PartyId = reader["PartyId"] == DBNull.Value ? null : Convert.ToInt32(reader["PartyId"]),
                PartyName = reader["PartyName"]?.ToString(), PartyMobile = reader["PartyMobile"]?.ToString(),
                TransporterName = reader["TransporterName"]?.ToString(), VehicleNo = reader["VehicleNo"]?.ToString() ?? "",
                VehicleTypeId = Convert.ToInt32(reader["VehicleTypeId"]), VehicleTypeName = reader["VehicleTypeName"]?.ToString(),
                DriverName = reader["DriverName"]?.ToString() ?? "", DriverMobile = reader["DriverMobile"]?.ToString(),
                ChallanNo = reader["ChallanNo"]?.ToString(), ApproxNoOfBags = Convert.ToInt32(reader["ApproxNoOfBags"]),
                ApproxWeight = Convert.ToDecimal(reader["ApproxWeight"]), PurposeRemarks = reader["PurposeRemarks"]?.ToString(),
                GateInDateTime = Convert.ToDateTime(reader["GateInDateTime"]), CreatedBy = Convert.ToInt32(reader["CreatedBy"]),
                GateManName = reader["GateManName"]?.ToString(), StatusName = reader["StatusName"]?.ToString() ?? "Pending",
                IsRSTGenerated = Convert.ToBoolean(reader["IsRSTGenerated"])
            };
        }

        public void PopulateItemCategoryNames(List<InwardHeader> entries)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT DISTINCT ge.InwardNo, c.CategoryName
                FROM dbo.t_GateEntry ge
                INNER JOIN dbo.m_ItemCategory c ON c.CategoryId = ge.ItemId
                WHERE ge.InwardNo IS NOT NULL
                ORDER BY ge.InwardNo, c.CategoryName", con);
            con.Open();
            using var reader = cmd.ExecuteReader();
            var categories = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                var inwardNo = reader.GetString(0);
                if (!categories.TryGetValue(inwardNo, out var names))
                    categories[inwardNo] = names = new List<string>();
                names.Add(reader.GetString(1));
            }
            foreach (var entry in entries)
                entry.ItemCategoryName = categories.TryGetValue(entry.InwardNo ?? "", out var names)
                    ? string.Join(", ", names) : string.Empty;
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
