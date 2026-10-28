using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class LocationMasterDAL
    {
        private readonly string _connectionString;

        public LocationMasterDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<Office> GetActiveOffices()
        {
            var offices = new List<Office>();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(
                "SELECT OfficeId, OfficeName FROM o05_office " +
                "WHERE IsActive = 1 AND OfficeType = 1 ORDER BY OfficeName", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                offices.Add(new Office
                {
                    OfficeId = Convert.ToInt32(reader["OfficeId"]),
                    OfficeName = reader["OfficeName"]?.ToString() ?? string.Empty
                });
            }
            return offices;
        }

        public List<LocationTypeOption> GetActiveLocationTypes()
        {
            var locationTypes = new List<LocationTypeOption>();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(
                "SELECT LoactionType_Id, LocationType_Name, Isactive " +
                "FROM dbo.tbl_locationType WHERE Isactive = 1 ORDER BY LoactionType_Id", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                locationTypes.Add(new LocationTypeOption
                {
                    LocationTypeId = Convert.ToInt32(reader["LoactionType_Id"]),
                    LocationTypeName = reader["LocationType_Name"]?.ToString() ?? string.Empty,
                    IsActive = Convert.ToBoolean(reader["Isactive"])
                });
            }
            return locationTypes;
        }

        public List<CompanyOption> GetActiveCompanies()
        {
            var companies = new List<CompanyOption>();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT CompanyId,OfficeId,CompanyName
                FROM dbo.tbl_company
                WHERE IsActive=1 AND OfficeId IS NOT NULL
                ORDER BY CompanyName", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                companies.Add(new CompanyOption
                {
                    CompanyId = Convert.ToInt32(reader["CompanyId"]),
                    OfficeId = Convert.ToInt32(reader["OfficeId"]),
                    CompanyName = reader["CompanyName"]?.ToString() ?? string.Empty
                });
            }
            return companies;
        }

        public List<SubCompanyOption> GetActiveSubCompanies(int companyId)
        {
            var subCompanies = new List<SubCompanyOption>();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT SubCompanyId,CompanyId,SubCompanyName
                FROM dbo.tbl_subcompany
                WHERE CompanyId=@CompanyId AND IsActive=1
                ORDER BY SubCompanyName", con);
            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyId;
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                subCompanies.Add(new SubCompanyOption
                {
                    SubCompanyId = Convert.ToInt32(reader["SubCompanyId"]),
                    CompanyId = Convert.ToInt32(reader["CompanyId"]),
                    SubCompanyName = reader["SubCompanyName"]?.ToString() ?? string.Empty
                });
            }
            return subCompanies;
        }

        public int GetOfficeIdForCompany(int companyId)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(OfficeId,0) FROM dbo.tbl_company WHERE CompanyId=@CompanyId AND IsActive=1", con);
            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyId;
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        public bool IsSubCompanyInCompany(int subCompanyId, int companyId)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM dbo.tbl_subcompany
                WHERE SubCompanyId=@SubCompanyId AND CompanyId=@CompanyId AND IsActive=1", con);
            cmd.Parameters.Add("@SubCompanyId", SqlDbType.Int).Value = subCompanyId;
            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyId;
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public List<LocationMaster> GetCompanyUnloadingLocations(int officeId)
        {
            var locations = new List<LocationMaster>();
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT l.LocationId,l.LocationCode,l.LocationName,l.LocationType,l.IsActive
                FROM dbo.m_LocationMaster l
                INNER JOIN dbo.t_OfficeLocationMapping m ON m.LocationId=l.LocationId
                WHERE m.OfficeId=@OfficeId AND l.IsActive=1 AND m.IsActive=1
                  AND ISNULL(m.UnloadingAllowed,1)=1
                ORDER BY l.LocationName;", con);
            cmd.Parameters.Add("@OfficeId", SqlDbType.Int).Value = officeId;
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                locations.Add(new LocationMaster
                {
                    LocationId = Convert.ToInt32(reader["LocationId"]),
                    LocationCode = reader["LocationCode"]?.ToString() ?? "",
                    LocationName = reader["LocationName"]?.ToString() ?? "",
                    LocationType = reader["LocationType"]?.ToString() ?? "",
                    IsActive = true
                });
            }
            return locations;
        }

        public List<LocationSubcompanyOption> GetLocationSubcompanies()
        {
            using var con=new SqlConnection(_connectionString);
            using var cmd=new SqlCommand(@"SELECT s.SubCompanyId,c.OfficeId,s.SubCompanyName FROM dbo.tbl_subcompany s
                JOIN dbo.tbl_company c ON c.CompanyId=s.CompanyId JOIN dbo.o05_office o ON o.OfficeId=c.OfficeId
                WHERE s.IsActive=1 AND c.IsActive=1 AND o.IsActive=1 AND o.OfficeType=1 ORDER BY s.SubCompanyName",con);
            con.Open();using var reader=cmd.ExecuteReader();var result=new List<LocationSubcompanyOption>();
            while(reader.Read())result.Add(new(reader.GetInt32(0),reader.GetInt32(1),reader.GetString(2)));return result;
        }

        public List<LocationMaster> GetAllLocations()
        {
            var locations = new List<LocationMaster>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageLocationMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT_ALL");
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            locations.Add(new LocationMaster
                            {
                                LocationId = Convert.ToInt32(rdr["LocationId"]),
                                CompanyId = rdr["CompanyId"] != DBNull.Value ? Convert.ToInt32(rdr["CompanyId"]) : 0,
                                CompanyName = rdr["CompanyName"]?.ToString(),
                                SubCompanyId = rdr["SubCompanyId"] != DBNull.Value ? Convert.ToInt32(rdr["SubCompanyId"]) : 0,
                                SubCompanyName = rdr["SubCompanyName"]?.ToString(),
                                OfficeId = rdr["OfficeId"] != DBNull.Value ? Convert.ToInt32(rdr["OfficeId"]) : 0,
                                OfficeName = rdr["OfficeName"]?.ToString(),
                                LocationCode = rdr["LocationCode"]?.ToString(),
                                LocationName = rdr["LocationName"]?.ToString() ?? "",
                                LocationType = rdr["LocationType"]?.ToString() ?? "",
                                Capacity = rdr["Capacity"] != DBNull.Value ? Convert.ToDecimal(rdr["Capacity"]) : 0,
                                CapacityUnit = rdr["CapacityUnit"]?.ToString() ?? "MT",
                                Description = rdr["Description"]?.ToString(),
                                Address = rdr["Address"]?.ToString(),
                                Remarks = rdr["Remarks"]?.ToString(),
                                IsActive = rdr["IsActive"] != DBNull.Value && Convert.ToBoolean(rdr["IsActive"])
                            });
                        }
                    }
                }
            }
            return locations;
        }

        public LocationMaster? GetLocationById(int locationId)
        {
            LocationMaster? loc = null;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageLocationMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT_BY_ID");
                    cmd.Parameters.AddWithValue("@LocationId", locationId);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            loc = new LocationMaster
                            {
                                LocationId = Convert.ToInt32(rdr["LocationId"]),
                                CompanyId = rdr["CompanyId"] != DBNull.Value ? Convert.ToInt32(rdr["CompanyId"]) : 0,
                                CompanyName = rdr["CompanyName"]?.ToString(),
                                SubCompanyId = rdr["SubCompanyId"] != DBNull.Value ? Convert.ToInt32(rdr["SubCompanyId"]) : 0,
                                SubCompanyName = rdr["SubCompanyName"]?.ToString(),
                                OfficeId = rdr["OfficeId"] != DBNull.Value ? Convert.ToInt32(rdr["OfficeId"]) : 0,
                                OfficeName = rdr["OfficeName"]?.ToString(),
                                LocationCode = rdr["LocationCode"]?.ToString(),
                                LocationName = rdr["LocationName"]?.ToString() ?? "",
                                LocationType = rdr["LocationType"]?.ToString() ?? "",
                                Capacity = rdr["Capacity"] != DBNull.Value ? Convert.ToDecimal(rdr["Capacity"]) : 0,
                                CapacityUnit = rdr["CapacityUnit"]?.ToString() ?? "MT",
                                Description = rdr["Description"]?.ToString(),
                                Address = rdr["Address"]?.ToString(),
                                Remarks = rdr["Remarks"]?.ToString(),
                                IsActive = rdr["IsActive"] != DBNull.Value && Convert.ToBoolean(rdr["IsActive"])
                            };
                        }
                    }
                }
            }
            return loc;
        }

        public bool SaveLocation(LocationMaster model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageLocationMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", model.LocationId == 0 ? "INSERT" : "UPDATE");
                    if (model.LocationId > 0) cmd.Parameters.AddWithValue("@LocationId", model.LocationId);
                    cmd.Parameters.Add("@OfficeId", SqlDbType.Int).Value = model.OfficeId;
                    cmd.Parameters.Add("@SubCompanyId", SqlDbType.Int).Value = model.SubCompanyId > 0 ? model.SubCompanyId : DBNull.Value;
                    cmd.Parameters.AddWithValue("@LocationName", model.LocationName);
                    cmd.Parameters.AddWithValue("@LocationType", model.LocationType);
                    cmd.Parameters.AddWithValue("@Capacity", model.Capacity);
                    cmd.Parameters.AddWithValue("@CapacityUnit", model.CapacityUnit ?? "MT");
                    cmd.Parameters.AddWithValue("@Description", (object?)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Remarks", (object?)model.Remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public bool DeleteLocation(int locationId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageLocationMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@LocationId", locationId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        // ====================================================================
        // OFFICE - LOCATION MAPPING METHODS
        // ====================================================================
        public List<OfficeLocationMapping> GetAllOfficeLocationMappings()
        {
            var mappings = new List<OfficeLocationMapping>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageOfficeLocationMapping", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT_ALL");
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            mappings.Add(new OfficeLocationMapping
                            {
                                MappingId = Convert.ToInt32(rdr["MappingId"]),
                                OfficeId = Convert.ToInt32(rdr["OfficeId"]),
                                OfficeName = rdr["OfficeName"]?.ToString(),
                                LocationId = Convert.ToInt32(rdr["LocationId"]),
                                LocationName = rdr["LocationName"]?.ToString(),
                                LocationCode = rdr["LocationCode"]?.ToString(),
                                UnloadingAllowed = Convert.ToBoolean(rdr["UnloadingAllowed"]),
                                EffectiveFrom = Convert.ToDateTime(rdr["EffectiveFrom"]),
                                IsActive = Convert.ToBoolean(rdr["IsActive"])
                            });
                        }
                    }
                }
            }
            return mappings;
        }

        public bool SaveOfficeLocationMapping(OfficeLocationMapping model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageOfficeLocationMapping", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", model.MappingId == 0 ? "INSERT" : "UPDATE");
                    if (model.MappingId > 0) cmd.Parameters.AddWithValue("@MappingId", model.MappingId);
                    cmd.Parameters.AddWithValue("@OfficeId", model.OfficeId);
                    cmd.Parameters.AddWithValue("@LocationId", model.LocationId);
                    cmd.Parameters.AddWithValue("@UnloadingAllowed", model.UnloadingAllowed);
                    cmd.Parameters.AddWithValue("@EffectiveFrom", model.EffectiveFrom);
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public bool DeleteOfficeLocationMapping(int mappingId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageOfficeLocationMapping", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@MappingId", mappingId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }
    }
}
