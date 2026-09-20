using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class VehicleDAL
    {
        private readonly string _connectionString;

        public VehicleDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<Vehicle> GetAllVehicles()
        {
            var vehicles = new List<Vehicle>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetAllVehicles", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vehicles.Add(new Vehicle
                            {
                                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                                VehicleNumber = reader["VehicleNumber"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return vehicles;
        }

        public int InsertVehicle(Vehicle vehicle)
        {
            int vehicleId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertVehicle", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VehicleNumber", vehicle.VehicleNumber);
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        vehicleId = Convert.ToInt32(result);
                    }
                }
            }
            return vehicleId;
        }

        public int InsertVehicleWithMapping(Vehicle vehicle)
        {
            using var con = new SqlConnection(_connectionString);
            con.Open();
            using var transaction = con.BeginTransaction();
            try
            {
                int vehicleId;
                using (var vehicleCmd = new SqlCommand("sp_InsertVehicle", con, transaction))
                {
                    vehicleCmd.CommandType = CommandType.StoredProcedure;
                    vehicleCmd.Parameters.Add("@VehicleNumber", SqlDbType.NVarChar, 30).Value = vehicle.VehicleNumber;
                    vehicleId = Convert.ToInt32(vehicleCmd.ExecuteScalar() ?? 0);
                }

                if (vehicleId <= 0) throw new InvalidOperationException("Vehicle could not be saved.");

                using (var mappingCmd = new SqlCommand("sp_InsertVehicleMapping", con, transaction))
                {
                    mappingCmd.CommandType = CommandType.StoredProcedure;
                    mappingCmd.Parameters.Add("@VehicleId", SqlDbType.Int).Value = vehicleId;
                    mappingCmd.Parameters.Add("@PartyId", SqlDbType.Int).Value = vehicle.PartyId;
                    mappingCmd.Parameters.Add("@DriverId", SqlDbType.Int).Value = vehicle.DriverId;
                    mappingCmd.ExecuteNonQuery();
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

        public bool UpdateVehicle(Vehicle vehicle)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_UpdateVehicle", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId);
                    cmd.Parameters.AddWithValue("@VehicleNumber", vehicle.VehicleNumber);
                    cmd.Parameters.AddWithValue("@IsActive", vehicle.IsActive);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }

        public void InsertVehicleMapping(int vehicleId, int partyId, int driverId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertVehicleMapping", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
