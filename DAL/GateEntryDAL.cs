using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            _connectionString =
                configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        // ============================================================
        // GET ALL GATE ENTRIES
        // ============================================================

        public List<GateEntry> GetAllGateEntries()
        {
            var entries = new List<GateEntry>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd =
                       new SqlCommand(
                           "sp_GetAllGateEntries",
                           con))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    con.Open();

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(
                                new GateEntry
                                {
                                    RSTNumber =
                                        reader["RSTNumber"]
                                            ?.ToString()
                                        ?? "",

                                    InwardNo =
                                        reader["InwardNo"]
                                            ?.ToString()
                                        ?? "",

                                    GrossWeight =
                                        reader["GrossWeight"]
                                            != DBNull.Value
                                            ? Convert.ToDecimal(
                                                reader["GrossWeight"])
                                            : 0,

                                    ItemId =
                                        HasColumn(reader, "ItemId")
                                        && reader["ItemId"]
                                            != DBNull.Value
                                            ? Convert.ToInt32(
                                                reader["ItemId"])
                                            : 0,

                                    ItemName =
                                        HasColumn(reader, "ItemName")
                                        && reader["ItemName"]
                                            != DBNull.Value
                                            ? reader["ItemName"]
                                                ?.ToString()
                                              ?? ""
                                            : "",

                                    TareWeight =
                                        reader["TareWeight"]
                                            != DBNull.Value
                                            ? Convert.ToDecimal(
                                                reader["TareWeight"])
                                            : null,

                                    NetWeight =
                                        reader["NetWeight"]
                                            != DBNull.Value
                                            ? Convert.ToDecimal(
                                                reader["NetWeight"])
                                            : null,

                                    TargetOfficeId =
                                        reader["TargetOfficeId"]
                                            != DBNull.Value
                                            ? Convert.ToInt32(
                                                reader["TargetOfficeId"])
                                            : null,

                                    GateEntryTime =
                                        reader["GateEntryTime"]
                                            != DBNull.Value
                                            ? Convert.ToDateTime(
                                                reader["GateEntryTime"])
                                            : DateTime.MinValue,

                                    GateExitTime =
                                        reader["GateExitTime"]
                                            != DBNull.Value
                                            ? Convert.ToDateTime(
                                                reader["GateExitTime"])
                                            : null,

                                    Status =
                                        reader["Status"]
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

                                    DriverName =
                                        reader["DriverName"]
                                            ?.ToString()
                                        ?? "",

                                    DriverMobile =
                                        reader["DriverMobile"]
                                            ?.ToString()
                                        ?? "",

                                    TotalBags =
                                        reader["TotalBags"]
                                            != DBNull.Value
                                            ? Convert.ToInt32(
                                                reader["TotalBags"])
                                            : null,

                                    WeighmentCharge =
                                        reader["WeighmentCharge"]
                                            != DBNull.Value
                                            ? Convert.ToDecimal(
                                                reader["WeighmentCharge"])
                                            : 0,

                                    InwardOutward =
                                        reader["InwardOutward"]
                                            ?.ToString()
                                        ?? "Inward",

                                    GateManName =
                                        reader["GateManName"]
                                            ?.ToString()
                                        ?? "Weightman"
                                }
                            );
                        }
                    }
                }
            }

            return entries;
        }

        // ============================================================
        // PENDING INWARDS FOR RST
        // ============================================================

        public List<PendingRstInward> GetPendingRstInwards()
        {
            var rows =
                new List<PendingRstInward>();

            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_GetPendingRstInwards",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            con.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(
                    new PendingRstInward
                    {
                        InwardNo =
                            reader["InwardNo"]
                                ?.ToString()
                            ?? "",

                        PartyId =
                            reader["PartyId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    reader["PartyId"])
                                : 0,

                        PartyName =
                            reader["PartyName"]
                                ?.ToString()
                            ?? "",

                        VehicleNumber =
                            reader["VehicleNo"]
                                ?.ToString()
                            ?? "",

                        DriverName =
                            reader["DriverName"]
                                ?.ToString()
                            ?? "",

                        DriverMobile =
                            reader["DriverMobile"]
                                ?.ToString()
                            ?? ""
                    }
                );
            }

            return rows;
        }

        // ============================================================
        // CREATE RST FROM INWARD
        // ============================================================

        public string CreateGateEntry(
            GateEntry entry)
        {
            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_CreateRstFromInward",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@InwardNo",
                SqlDbType.NVarChar,
                50
            ).Value =
                entry.InwardNo ?? "";

            cmd.Parameters.Add(
                "@GrossWeight",
                SqlDbType.Decimal
            ).Value =
                entry.GrossWeight;

            cmd.Parameters.Add(
                "@TargetOfficeId",
                SqlDbType.Int
            ).Value =
                (object?)entry.TargetOfficeId
                ?? DBNull.Value;

            con.Open();

            object? result =
                cmd.ExecuteScalar();

            string rstNumber =
                result?.ToString()
                ?? "";

            if (string.IsNullOrWhiteSpace(
                rstNumber))
            {
                throw new InvalidOperationException(
                    "RST was not generated."
                );
            }

            entry.RSTNumber =
                rstNumber;


            // ========================================================
            // MULTIPLE LOCATIONS
            // Other developer functionality preserved
            // ========================================================

            if (entry.TargetLocationIds != null &&
                entry.TargetLocationIds.Count > 0)
            {
                foreach (
                    int locationId
                    in entry.TargetLocationIds
                        .Where(x => x > 0)
                        .Distinct())
                {
                    using SqlCommand locationCmd =
                        new SqlCommand(
                            "sp_InsertGateEntryLocation",
                            con);

                    locationCmd.CommandType =
                        CommandType.StoredProcedure;

                    locationCmd.Parameters.Add(
                        "@RSTNumber",
                        SqlDbType.NVarChar,
                        100
                    ).Value =
                        rstNumber;

                    locationCmd.Parameters.Add(
                        "@LocationId",
                        SqlDbType.Int
                    ).Value =
                        locationId;

                    locationCmd.ExecuteNonQuery();
                }
            }

            return rstNumber;
        }

        // ============================================================
        // GET DRIVERS WITH MOBILE
        // ============================================================

        public List<dynamic> GetDriversWithMobile()
        {
            var list =
                new List<dynamic>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT
                        PersonId,
                        PersonName,
                        ISNULL(MobileNumber, '') AS MobileNumber
                    FROM dbo.P02_Person
                    WHERE IsActive = 1
                      AND
                      (
                          PersonType = 'Driver'
                          OR PersonType IS NULL
                          OR TRIM(PersonType) = ''
                      );";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    con.Open();

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name =
                                reader["PersonName"]
                                    ?.ToString()
                                ?? "";

                            string mobile =
                                reader["MobileNumber"]
                                    ?.ToString()
                                ?? "";

                            string displayName =
                                string.IsNullOrWhiteSpace(
                                    mobile)
                                    ? name
                                    : $"{name} / {mobile}";

                            list.Add(
                                new
                                {
                                    DriverId =
                                        Convert.ToInt32(
                                            reader["PersonId"]),

                                    DriverNameMobile =
                                        displayName
                                }
                            );
                        }
                    }
                }
            }

            if (list.Count == 0)
            {
                list.Add(
                    new
                    {
                        DriverId = 1,
                        DriverNameMobile =
                            "Default Driver / 9876543210"
                    }
                );
            }

            return list;
        }

        // ============================================================
        // GET INWARD DETAILS BY INWARD NUMBER
        // ============================================================

        public DataTable GetInwardDetails(
            string inwardNo)
        {
            var dt =
                new DataTable();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT
                        h.InwardNo,
                        h.PartyId,
                        ISNULL(
                            NULLIF(TRIM(h.PartyName), ''),
                            p.PersonName
                        ) AS PartyName,
                        h.VehicleNo,
                        v.VehicleId,
                        h.DriverName,
                        h.DriverMobile,
                        d.PersonId AS DriverId
                    FROM dbo.t_InwardHeader h

                    LEFT JOIN dbo.P02_Person p
                        ON h.PartyId = p.PersonId

                    LEFT JOIN dbo.m_Vehicle v
                        ON UPPER(TRIM(h.VehicleNo))
                           =
                           UPPER(TRIM(v.VehicleNumber))

                    LEFT JOIN dbo.P02_Person d
                        ON LOWER(TRIM(h.DriverName))
                           =
                           LOWER(TRIM(d.PersonName))

                    WHERE TRIM(h.InwardNo)
                          =
                          TRIM(@InwardNo)

                      AND ISNULL(
                          h.RSTEntryCompleted,
                          0
                      ) = 0;";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    cmd.Parameters.Add(
                        "@InwardNo",
                        SqlDbType.NVarChar,
                        100
                    ).Value =
                        inwardNo ?? "";

                    using SqlDataAdapter da =
                        new SqlDataAdapter(cmd);

                    da.Fill(dt);
                }
            }

            return dt;
        }

        // ============================================================
        // GET INWARD DETAILS BY ID
        // ============================================================

        public DataTable GetInwardDetailsById(
            int inwardId)
        {
            var dt =
                new DataTable();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT
                        h.InwardId,
                        h.InwardNo,
                        h.PartyId,

                        ISNULL(
                            NULLIF(TRIM(h.PartyName), ''),
                            p.PersonName
                        ) AS PartyName,

                        h.VehicleNo,
                        v.VehicleId,
                        h.DriverName,
                        h.DriverMobile,
                        d.PersonId AS DriverId,
                        h.ApproxNoOfBags,
                        h.ApproxWeight

                    FROM dbo.t_InwardHeader h

                    INNER JOIN dbo.m_InwardTypeMaster it
                        ON it.InwardTypeId =
                           h.InwardTypeId

                    LEFT JOIN dbo.P02_Person p
                        ON h.PartyId =
                           p.PersonId

                    LEFT JOIN dbo.m_Vehicle v
                        ON UPPER(TRIM(h.VehicleNo))
                           =
                           UPPER(TRIM(v.VehicleNumber))

                    LEFT JOIN dbo.P02_Person d
                        ON LOWER(TRIM(h.DriverName))
                           =
                           LOWER(TRIM(d.PersonName))

                    WHERE h.InwardId =
                          @InwardId

                      AND it.RSTRequired =
                          1

                      AND ISNULL(
                          h.RSTEntryCompleted,
                          0
                      ) = 0;";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    cmd.Parameters.Add(
                        "@InwardId",
                        SqlDbType.Int
                    ).Value =
                        inwardId;

                    using SqlDataAdapter da =
                        new SqlDataAdapter(cmd);

                    da.Fill(dt);
                }
            }

            return dt;
        }

        // ============================================================
        // PARTY LINKAGE
        // ============================================================

        public dynamic GetLinkageByParty(
            int partyId)
        {
            int vehicleId = 0;
            int driverId = 0;

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT TOP 1
                        VehicleId,
                        DriverId
                    FROM dbo.t_GateEntry
                    WHERE PartyId = @PartyId
                    ORDER BY GateEntryTime DESC;";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    cmd.Parameters.Add(
                        "@PartyId",
                        SqlDbType.Int
                    ).Value =
                        partyId;

                    con.Open();

                    using SqlDataReader rdr =
                        cmd.ExecuteReader();

                    if (rdr.Read())
                    {
                        vehicleId =
                            rdr["VehicleId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    rdr["VehicleId"])
                                : 0;

                        driverId =
                            rdr["DriverId"]
                                != DBNull.Value
                                ? Convert.ToInt32(
                                    rdr["DriverId"])
                                : 0;
                    }
                }
            }

            return new
            {
                vehicleId,
                driverId
            };
        }

        // ============================================================
        // COMPLETE GATE EXIT
        // ============================================================

        public bool CompleteGateExit(
            string rstNumber,
            decimal tareWeight)
        {
            int rowsAffected = 0;

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd =
                       new SqlCommand(
                           "sp_CompleteGateExit",
                           con))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.Add(
                        "@RSTNumber",
                        SqlDbType.NVarChar,
                        100
                    ).Value =
                        rstNumber;

                    cmd.Parameters.Add(
                        "@TareWeight",
                        SqlDbType.Decimal
                    ).Value =
                        tareWeight;

                    con.Open();

                    rowsAffected =
                        cmd.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        // ============================================================
        // VEHICLES BY PARTY
        // ============================================================

        public List<dynamic> GetVehiclesByParty(
            int partyId)
        {
            var list =
                new List<dynamic>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT DISTINCT
                        v.VehicleId,
                        v.VehicleNumber,

                        CASE
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM dbo.t_GateEntry ge
                                WHERE ge.PartyId =
                                      @PartyId
                                  AND ge.VehicleId =
                                      v.VehicleId

                                UNION

                                SELECT 1
                                FROM dbo.t_InwardHeader ih
                                WHERE ih.PartyId =
                                      @PartyId
                                  AND UPPER(
                                      TRIM(ih.VehicleNo)
                                  )
                                  =
                                  UPPER(
                                      TRIM(v.VehicleNumber)
                                  )
                            )
                            THEN 1
                            ELSE 0
                        END AS IsLinked

                    FROM dbo.m_Vehicle v

                    WHERE v.IsActive = 1

                    ORDER BY
                        IsLinked DESC,
                        v.VehicleNumber ASC;";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    cmd.Parameters.Add(
                        "@PartyId",
                        SqlDbType.Int
                    ).Value =
                        partyId;

                    con.Open();

                    using SqlDataReader rdr =
                        cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        list.Add(
                            new
                            {
                                VehicleId =
                                    Convert.ToInt32(
                                        rdr["VehicleId"]),

                                VehicleNumber =
                                    rdr["VehicleNumber"]
                                        ?.ToString()
                                    ?? "",

                                IsLinked =
                                    Convert.ToInt32(
                                        rdr["IsLinked"]
                                    ) == 1
                            }
                        );
                    }
                }
            }

            return list;
        }

        // ============================================================
        // DRIVERS BY PARTY
        // ============================================================

        public List<dynamic> GetDriversByParty(
            int partyId)
        {
            var list =
                new List<dynamic>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT DISTINCT
                        p.PersonId AS DriverId,

                        CASE
                            WHEN p.MobileNumber IS NULL
                              OR TRIM(p.MobileNumber) = ''
                            THEN p.PersonName
                            ELSE
                                p.PersonName
                                + ' / '
                                + p.MobileNumber
                        END AS DriverNameMobile,

                        CASE
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM dbo.t_GateEntry ge
                                WHERE ge.PartyId =
                                      @PartyId
                                  AND ge.DriverId =
                                      p.PersonId

                                UNION

                                SELECT 1
                                FROM dbo.t_InwardHeader ih
                                WHERE ih.PartyId =
                                      @PartyId
                                  AND LOWER(
                                      TRIM(ih.DriverName)
                                  )
                                  =
                                  LOWER(
                                      TRIM(p.PersonName)
                                  )
                            )
                            THEN 1
                            ELSE 0
                        END AS IsLinked

                    FROM dbo.P02_Person p

                    WHERE p.IsActive = 1

                      AND
                      (
                          p.PersonType = 'Driver'
                          OR p.PersonType IS NULL
                          OR TRIM(p.PersonType) = ''
                      )

                    ORDER BY
                        IsLinked DESC,
                        DriverNameMobile ASC;";

                using (SqlCommand cmd =
                       new SqlCommand(
                           query,
                           con))
                {
                    cmd.Parameters.Add(
                        "@PartyId",
                        SqlDbType.Int
                    ).Value =
                        partyId;

                    con.Open();

                    using SqlDataReader rdr =
                        cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        list.Add(
                            new
                            {
                                DriverId =
                                    Convert.ToInt32(
                                        rdr["DriverId"]),

                                DriverNameMobile =
                                    rdr["DriverNameMobile"]
                                        ?.ToString()
                                    ?? "",

                                IsLinked =
                                    Convert.ToInt32(
                                        rdr["IsLinked"]
                                    ) == 1
                            }
                        );
                    }
                }
            }

            return list;
        }

        // ============================================================
        // HELPER
        // ============================================================

        private static bool HasColumn(
            SqlDataReader reader,
            string columnName)
        {
            for (int i = 0;
                 i < reader.FieldCount;
                 i++)
            {
                if (reader
                    .GetName(i)
                    .Equals(
                        columnName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}