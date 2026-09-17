using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class OfficeDAL
    {
        private readonly string _connectionString;

        public OfficeDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<Office> GetAllOffices()
        {
            var offices = new List<Office>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetAllOffices", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            offices.Add(new Office
                            {
                                OfficeId = Convert.ToInt32(reader["OfficeId"]),
                                OfficeName = reader["OfficeName"].ToString() ?? "",
                                Location = reader["Location"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return offices;
        }

        public List<OfficeLocation> GetAllLocations()
        {
            var locations = new List<OfficeLocation>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT l.LocationId, ISNULL(m.OfficeId, 1) AS OfficeId, l.LocationName 
                    FROM m_LocationMaster l
                    LEFT JOIN t_OfficeLocationMapping m ON l.LocationId = m.LocationId
                    WHERE l.IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new OfficeLocation
                            {
                                LocationId = Convert.ToInt32(reader["LocationId"]),
                                OfficeId = Convert.ToInt32(reader["OfficeId"]),
                                LocationName = reader["LocationName"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            return locations;
        }

        public int InsertOffice(Office office)
        {
            int officeId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertOffice", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OfficeName", office.OfficeName);
                    cmd.Parameters.AddWithValue("@Location", ""); // Deprecated column
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        officeId = Convert.ToInt32(result);
                    }
                }

                if (officeId > 0 && !string.IsNullOrWhiteSpace(office.Location))
                {
                    var locations = office.Location.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var loc in locations)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertOfficeLocation", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@OfficeId", officeId);
                            cmd.Parameters.AddWithValue("@LocationName", loc.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            return officeId;
        }

        public bool UpdateOffice(Office office)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_UpdateOffice", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OfficeId", office.OfficeId);
                    cmd.Parameters.AddWithValue("@OfficeName", office.OfficeName);
                    cmd.Parameters.AddWithValue("@Location", office.Location);
                    cmd.Parameters.AddWithValue("@IsActive", office.IsActive);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }
    }
}
