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
