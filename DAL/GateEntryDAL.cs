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

            // ItemId on gate entries stores the category ID in this workflow.
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT ge.RSTNumber, c.CategoryName
                FROM dbo.t_GateEntry ge
                INNER JOIN dbo.m_ItemCategory c ON c.CategoryId = ge.ItemId", con))
            {
                con.Open();
                using var reader = cmd.ExecuteReader();
                var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                    categories[reader.GetString(0)] = reader.GetString(1);
                foreach (var entry in entries)
                    entry.ItemCategoryName = categories.GetValueOrDefault(entry.RSTNumber, string.Empty);
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

        public DataTable GetItemCategories()
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT CategoryId, CategoryName FROM dbo.m_ItemCategory WHERE IsActive = 1 ORDER BY CategoryName", con);
            using var adapter = new SqlDataAdapter(cmd);
            var categories = new DataTable();
            adapter.Fill(categories);
            return categories;
        }

        public string CreateGateEntry(
            GateEntry entry)
        {
            using SqlConnection con =
                new SqlConnection(_connectionString);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_CreateGateEntry",
                    con);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@InwardNo",
                SqlDbType.NVarChar,
                50
            ).Value =
                entry.InwardNo ?? "";

            cmd.Parameters.Add("@PartyId", SqlDbType.Int).Value =
                entry.PartyId > 0 ? entry.PartyId : DBNull.Value;

            cmd.Parameters.Add("@VehicleId", SqlDbType.Int).Value =
                entry.VehicleId > 0 ? entry.VehicleId : DBNull.Value;

            cmd.Parameters.Add("@DriverId", SqlDbType.Int).Value =
                entry.DriverId > 0 ? entry.DriverId : DBNull.Value;

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

            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value =
                entry.ItemCategoryId > 0 ? entry.ItemCategoryId : DBNull.Value;

            //cmd.Parameters.Add("@ItemCategoryId", SqlDbType.Int).Value = entry.ItemCategoryId;

            var chargeParameter = cmd.Parameters.Add("@WeightCharge", SqlDbType.Decimal);
            chargeParameter.Precision = 18;
            chargeParameter.Scale = 2;
            chargeParameter.Value = entry.WeighmentCharge;

            cmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value =
                entry.CreatedBy > 0 ? entry.CreatedBy : 1;

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

                    var tareParameter = cmd.Parameters.Add(
                        "@TareWeight",
                        SqlDbType.Decimal
                    );
                    tareParameter.Precision = 18;
                    tareParameter.Scale = 2;
                    tareParameter.Value = tareWeight;

                    con.Open();

                    rowsAffected =
                        cmd.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        // ============================================================
        // VEHICLES BY PARTY
        // Only vehicles actively linked with the selected Party
        // through m_VehicleMapping are returned.
        // ============================================================

        public List<dynamic> GetVehiclesByParty(
            int partyId)
        {
            var list =
                new List<dynamic>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                const string query = @"
                    SELECT DISTINCT
                        V.VehicleId,
                        V.VehicleNumber
                    FROM dbo.m_VehicleMapping VM
                    INNER JOIN dbo.m_Vehicle V
                        ON V.VehicleId = VM.VehicleId
                    WHERE
                        VM.PartyId = @PartyId
                        AND VM.IsActive = 1
                        AND V.IsActive = 1
                    ORDER BY
                        V.VehicleNumber;";

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
                                        rdr["VehicleId"]
                                    ),

                                VehicleNumber =
                                    rdr["VehicleNumber"]
                                        ?.ToString()
                                    ?? "",

                                IsLinked = true
                            }
                        );
                    }
                }
            }

            return list;
        }


        // ============================================================
        // DRIVERS BY VEHICLE
        // Party -> Vehicle -> Driver
        // ============================================================

        public List<dynamic> GetDriversByVehicle(
            int partyId,
            int vehicleId)
        {
            var list =
                new List<dynamic>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                const string query = @"
                    SELECT DISTINCT
                        D.PersonId AS DriverId,

                        CASE
                            WHEN D.MobileNumber IS NULL
                              OR TRIM(D.MobileNumber) = ''
                            THEN D.PersonName
                            ELSE
                                D.PersonName
                                + ' / '
                                + D.MobileNumber
                        END AS DriverNameMobile

                    FROM dbo.m_VehicleMapping VM

                    INNER JOIN dbo.P02_Person D
                        ON D.PersonId = VM.DriverId

                    WHERE
                        VM.PartyId = @PartyId
                        AND VM.VehicleId = @VehicleId
                        AND VM.IsActive = 1
                        AND D.IsActive = 1

                    ORDER BY
                        DriverNameMobile;";

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

                    cmd.Parameters.Add(
                        "@VehicleId",
                        SqlDbType.Int
                    ).Value =
                        vehicleId;

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
                                        rdr["DriverId"]
                                    ),

                                DriverNameMobile =
                                    rdr["DriverNameMobile"]
                                        ?.ToString()
                                    ?? "",

                                IsLinked = true
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
                          OR p.PersonType = '9'
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
        // QUICK ADD PARTY
        // Used by GateEntry/Create "Other" option.
        // Party post id = 8.
        // ============================================================

        public int QuickAddParty(
            string partyName,
            string? mobileNumber)
        {
            partyName =
                (partyName ?? string.Empty).Trim();

            mobileNumber =
                (mobileNumber ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(partyName))
            {
                throw new InvalidOperationException(
                    "Party name is required."
                );
            }

            using SqlConnection con =
                new SqlConnection(_connectionString);

            con.Open();

            using SqlTransaction transaction =
                con.BeginTransaction();

            try
            {
                int existingPartyId = 0;

                using (SqlCommand existingCmd =
                       new SqlCommand(@"
                    SELECT TOP 1
                        P.PersonId
                    FROM dbo.P02_Person P
                    INNER JOIN dbo.P11_PersonDesignatation PD
                        ON PD.P02_PersonId = P.PersonId
                       AND PD.IsActive = 1
                    INNER JOIN dbo.O12_Designatation D
                        ON D.DesignationId = PD.O12_DesignationId
                       AND D.IsActive = 1
                    WHERE
                        P.IsActive = 1
                        AND D.O10_PostId = 8
                        AND
                        (
                            (
                                @MobileNumber <> ''
                                AND ISNULL(
                                    TRIM(P.MobileNumber),
                                    ''
                                ) = @MobileNumber
                            )
                            OR
                            LOWER(
                                TRIM(P.PersonName)
                            ) =
                            LOWER(
                                @PartyName
                            )
                        )
                    ORDER BY
                        P.PersonId DESC;",
                           con,
                           transaction))
                {
                    existingCmd.Parameters.Add(
                        "@PartyName",
                        SqlDbType.NVarChar,
                        400
                    ).Value =
                        partyName;

                    existingCmd.Parameters.Add(
                        "@MobileNumber",
                        SqlDbType.NVarChar,
                        40
                    ).Value =
                        mobileNumber;

                    object? existingResult =
                        existingCmd.ExecuteScalar();

                    if (
                        existingResult != null
                        &&
                        existingResult != DBNull.Value
                    )
                    {
                        existingPartyId =
                            Convert.ToInt32(
                                existingResult
                            );
                    }
                }

                if (existingPartyId > 0)
                {
                    transaction.Commit();

                    return existingPartyId;
                }


                int designationId = 0;

                using (SqlCommand designationCmd =
                       new SqlCommand(@"
                    SELECT TOP 1
                        DesignationId
                    FROM dbo.O12_Designatation
                    WHERE
                        O10_PostId = 8
                        AND IsActive = 1
                    ORDER BY
                        DesignationId;",
                           con,
                           transaction))
                {
                    object? designationResult =
                        designationCmd.ExecuteScalar();

                    if (
                        designationResult != null
                        &&
                        designationResult != DBNull.Value
                    )
                    {
                        designationId =
                            Convert.ToInt32(
                                designationResult
                            );
                    }
                }

                if (designationId <= 0)
                {
                    throw new InvalidOperationException(
                        "Party designation is not configured for PostId 8."
                    );
                }


                int partyId;

                using (SqlCommand insertPersonCmd =
                       new SqlCommand(@"
                    INSERT INTO dbo.P02_Person
                    (
                        PersonName,
                        MobileNumber,
                        PersonType,
                        IsActive
                    )
                    VALUES
                    (
                        @PartyName,
                        NULLIF(
                            @MobileNumber,
                            ''
                        ),
                        '8',
                        1
                    );

                    SELECT
                        CAST(
                            SCOPE_IDENTITY()
                            AS INT
                        );",
                           con,
                           transaction))
                {
                    insertPersonCmd.Parameters.Add(
                        "@PartyName",
                        SqlDbType.NVarChar,
                        400
                    ).Value =
                        partyName;

                    insertPersonCmd.Parameters.Add(
                        "@MobileNumber",
                        SqlDbType.NVarChar,
                        40
                    ).Value =
                        mobileNumber;

                    partyId =
                        Convert.ToInt32(
                            insertPersonCmd.ExecuteScalar()
                            ?? 0
                        );
                }

                if (partyId <= 0)
                {
                    throw new InvalidOperationException(
                        "Party could not be saved."
                    );
                }


                using (SqlCommand mappingCmd =
                       new SqlCommand(@"
                    INSERT INTO dbo.P11_PersonDesignatation
                    (
                        P02_PersonId,
                        O12_DesignationId,
                        EffectiveFrom,
                        Addedon,
                        IsActive
                    )
                    VALUES
                    (
                        @PersonId,
                        @DesignationId,
                        GETDATE(),
                        GETDATE(),
                        1
                    );",
                           con,
                           transaction))
                {
                    mappingCmd.Parameters.Add(
                        "@PersonId",
                        SqlDbType.Int
                    ).Value =
                        partyId;

                    mappingCmd.Parameters.Add(
                        "@DesignationId",
                        SqlDbType.Int
                    ).Value =
                        designationId;

                    mappingCmd.ExecuteNonQuery();
                }


                transaction.Commit();

                return partyId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ============================================================
        // QUICK ADD VEHICLE
        // Vehicle is created first. Party/Driver mapping is created
        // after a driver is selected/added.
        // ============================================================

        public int QuickAddVehicle(
            int partyId,
            string vehicleNumber)
        {
            if (partyId <= 0)
            {
                throw new InvalidOperationException(
                    "Select Party first."
                );
            }

            vehicleNumber =
                (vehicleNumber ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant()
                    .Replace(
                        " ",
                        string.Empty
                    );

            if (string.IsNullOrWhiteSpace(vehicleNumber))
            {
                throw new InvalidOperationException(
                    "Vehicle number is required."
                );
            }

            using SqlConnection con =
                new SqlConnection(_connectionString);

            con.Open();

            using SqlTransaction transaction =
                con.BeginTransaction();

            try
            {
                using (SqlCommand partyCmd =
                       new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM dbo.P02_Person
                    WHERE
                        PersonId = @PartyId
                        AND IsActive = 1;",
                           con,
                           transaction))
                {
                    partyCmd.Parameters.Add(
                        "@PartyId",
                        SqlDbType.Int
                    ).Value =
                        partyId;

                    int partyExists =
                        Convert.ToInt32(
                            partyCmd.ExecuteScalar()
                            ?? 0
                        );

                    if (partyExists == 0)
                    {
                        throw new InvalidOperationException(
                            "Selected Party is not active."
                        );
                    }
                }


                int vehicleId = 0;

                using (SqlCommand existingCmd =
                       new SqlCommand(@"
                    SELECT TOP 1
                        VehicleId
                    FROM dbo.m_Vehicle
                    WHERE
                        IsActive = 1
                        AND
                        UPPER(
                            REPLACE(
                                TRIM(VehicleNumber),
                                ' ',
                                ''
                            )
                        ) = @VehicleNumber
                    ORDER BY
                        VehicleId DESC;",
                           con,
                           transaction))
                {
                    existingCmd.Parameters.Add(
                        "@VehicleNumber",
                        SqlDbType.NVarChar,
                        100
                    ).Value =
                        vehicleNumber;

                    object? existingResult =
                        existingCmd.ExecuteScalar();

                    if (
                        existingResult != null
                        &&
                        existingResult != DBNull.Value
                    )
                    {
                        vehicleId =
                            Convert.ToInt32(
                                existingResult
                            );
                    }
                }

                if (vehicleId <= 0)
                {
                    using SqlCommand insertCmd =
                        new SqlCommand(@"
                        INSERT INTO dbo.m_Vehicle
                        (
                            VehicleNumber,
                            IsActive
                        )
                        VALUES
                        (
                            @VehicleNumber,
                            1
                        );

                        SELECT
                            CAST(
                                SCOPE_IDENTITY()
                                AS INT
                            );",
                            con,
                            transaction);

                    insertCmd.Parameters.Add(
                        "@VehicleNumber",
                        SqlDbType.NVarChar,
                        100
                    ).Value =
                        vehicleNumber;

                    vehicleId =
                        Convert.ToInt32(
                            insertCmd.ExecuteScalar()
                            ?? 0
                        );
                }

                if (vehicleId <= 0)
                {
                    throw new InvalidOperationException(
                        "Vehicle could not be saved."
                    );
                }

                transaction.Commit();

                return vehicleId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ============================================================
        // QUICK ADD DRIVER + LINK PARTY/VEHICLE/DRIVER
        // Driver post id = 9 when designation exists.
        // ============================================================

        public int QuickAddDriver(
            int partyId,
            int vehicleId,
            string driverName,
            string? mobileNumber)
        {
            if (partyId <= 0)
            {
                throw new InvalidOperationException(
                    "Select Party first."
                );
            }

            if (vehicleId <= 0)
            {
                throw new InvalidOperationException(
                    "Select Vehicle first."
                );
            }

            driverName =
                (driverName ?? string.Empty).Trim();

            mobileNumber =
                (mobileNumber ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(driverName))
            {
                throw new InvalidOperationException(
                    "Driver name is required."
                );
            }

            using SqlConnection con =
                new SqlConnection(_connectionString);

            con.Open();

            using SqlTransaction transaction =
                con.BeginTransaction();

            try
            {
                int driverId = 0;

                using (SqlCommand existingCmd =
                       new SqlCommand(@"
                    SELECT TOP 1
                        PersonId
                    FROM dbo.P02_Person
                    WHERE
                        IsActive = 1
                        AND
                        (
                            PersonType = 'Driver'
                            OR PersonType = '9'
                        )
                        AND
                        (
                            (
                                @MobileNumber <> ''
                                AND ISNULL(
                                    TRIM(MobileNumber),
                                    ''
                                ) = @MobileNumber
                            )
                            OR
                            (
                                @MobileNumber = ''
                                AND LOWER(
                                    TRIM(PersonName)
                                ) =
                                LOWER(
                                    @DriverName
                                )
                            )
                        )
                    ORDER BY
                        PersonId DESC;",
                           con,
                           transaction))
                {
                    existingCmd.Parameters.Add(
                        "@DriverName",
                        SqlDbType.NVarChar,
                        400
                    ).Value =
                        driverName;

                    existingCmd.Parameters.Add(
                        "@MobileNumber",
                        SqlDbType.NVarChar,
                        40
                    ).Value =
                        mobileNumber;

                    object? existingResult =
                        existingCmd.ExecuteScalar();

                    if (
                        existingResult != null
                        &&
                        existingResult != DBNull.Value
                    )
                    {
                        driverId =
                            Convert.ToInt32(
                                existingResult
                            );
                    }
                }


                if (driverId <= 0)
                {
                    using SqlCommand insertPersonCmd =
                        new SqlCommand(@"
                        INSERT INTO dbo.P02_Person
                        (
                            PersonName,
                            MobileNumber,
                            PersonType,
                            IsActive
                        )
                        VALUES
                        (
                            @DriverName,
                            NULLIF(
                                @MobileNumber,
                                ''
                            ),
                            '9',
                            1
                        );

                        SELECT
                            CAST(
                                SCOPE_IDENTITY()
                                AS INT
                            );",
                            con,
                            transaction);

                    insertPersonCmd.Parameters.Add(
                        "@DriverName",
                        SqlDbType.NVarChar,
                        400
                    ).Value =
                        driverName;

                    insertPersonCmd.Parameters.Add(
                        "@MobileNumber",
                        SqlDbType.NVarChar,
                        40
                    ).Value =
                        mobileNumber;

                    driverId =
                        Convert.ToInt32(
                            insertPersonCmd.ExecuteScalar()
                            ?? 0
                        );


                    if (driverId <= 0)
                    {
                        throw new InvalidOperationException(
                            "Driver could not be saved."
                        );
                    }


                    int driverDesignationId = 0;

                    using (SqlCommand designationCmd =
                           new SqlCommand(@"
                        SELECT TOP 1
                            DesignationId
                        FROM dbo.O12_Designatation
                        WHERE
                            O10_PostId = 9
                            AND IsActive = 1
                        ORDER BY
                            DesignationId;",
                               con,
                               transaction))
                    {
                        object? designationResult =
                            designationCmd.ExecuteScalar();

                        if (
                            designationResult != null
                            &&
                            designationResult != DBNull.Value
                        )
                        {
                            driverDesignationId =
                                Convert.ToInt32(
                                    designationResult
                                );
                        }
                    }


                    if (driverDesignationId > 0)
                    {
                        using SqlCommand designationMapCmd =
                            new SqlCommand(@"
                            INSERT INTO dbo.P11_PersonDesignatation
                            (
                                P02_PersonId,
                                O12_DesignationId,
                                EffectiveFrom,
                                Addedon,
                                IsActive
                            )
                            VALUES
                            (
                                @PersonId,
                                @DesignationId,
                                GETDATE(),
                                GETDATE(),
                                1
                            );",
                                con,
                                transaction);

                        designationMapCmd.Parameters.Add(
                            "@PersonId",
                            SqlDbType.Int
                        ).Value =
                            driverId;

                        designationMapCmd.Parameters.Add(
                            "@DesignationId",
                            SqlDbType.Int
                        ).Value =
                            driverDesignationId;

                        designationMapCmd.ExecuteNonQuery();
                    }
                }


                EnsureVehicleMapping(
                    con,
                    transaction,
                    partyId,
                    vehicleId,
                    driverId
                );


                transaction.Commit();

                return driverId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ============================================================
        // LINK AN EXISTING DRIVER WITH PARTY + VEHICLE
        // ============================================================

        public void EnsureVehicleMapping(
            int partyId,
            int vehicleId,
            int driverId)
        {
            if (
                partyId <= 0
                ||
                vehicleId <= 0
                ||
                driverId <= 0
            )
            {
                throw new InvalidOperationException(
                    "Valid Party, Vehicle and Driver are required."
                );
            }

            using SqlConnection con =
                new SqlConnection(_connectionString);

            con.Open();

            using SqlTransaction transaction =
                con.BeginTransaction();

            try
            {
                EnsureVehicleMapping(
                    con,
                    transaction,
                    partyId,
                    vehicleId,
                    driverId
                );

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        private static void EnsureVehicleMapping(
            SqlConnection con,
            SqlTransaction transaction,
            int partyId,
            int vehicleId,
            int driverId)
        {
            using SqlCommand cmd =
                new SqlCommand(@"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.m_VehicleMapping
                    WHERE
                        PartyId = @PartyId
                        AND VehicleId = @VehicleId
                        AND DriverId = @DriverId
                        AND IsActive = 1
                )
                BEGIN
                    INSERT INTO dbo.m_VehicleMapping
                    (
                        VehicleId,
                        PartyId,
                        DriverId,
                        IsActive
                    )
                    VALUES
                    (
                        @VehicleId,
                        @PartyId,
                        @DriverId,
                        1
                    );
                END;",
                    con,
                    transaction);

            cmd.Parameters.Add(
                "@PartyId",
                SqlDbType.Int
            ).Value =
                partyId;

            cmd.Parameters.Add(
                "@VehicleId",
                SqlDbType.Int
            ).Value =
                vehicleId;

            cmd.Parameters.Add(
                "@DriverId",
                SqlDbType.Int
            ).Value =
                driverId;

            cmd.ExecuteNonQuery();
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
