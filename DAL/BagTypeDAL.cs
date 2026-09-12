using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class BagTypeDAL
    {
        private readonly string _connectionString;

        public BagTypeDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<BagType> GetAllBagTypes()
        {
            var bags = new List<BagType>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetAllBagTypes", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bags.Add(new BagType
                            {
                                BagTypeId = Convert.ToInt32(reader["BagTypeId"]),
                                BagTypeName = reader["BagTypeName"].ToString() ?? "",
                                DeductionWeightGrams = Convert.ToInt32(reader["DeductionWeightGrams"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return bags;
        }

        public int InsertBagType(BagType bag)
        {
            int bagId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertBagType", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BagTypeName", bag.BagTypeName);
                    cmd.Parameters.AddWithValue("@DeductionWeightGrams", bag.DeductionWeightGrams);
                    
                    con.Open();
                    object? result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        bagId = Convert.ToInt32(result);
                    }
                }
            }
            return bagId;
        }

        public bool UpdateBagType(BagType bag)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_UpdateBagType", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BagTypeId", bag.BagTypeId);
                    cmd.Parameters.AddWithValue("@BagTypeName", bag.BagTypeName);
                    cmd.Parameters.AddWithValue("@DeductionWeightGrams", bag.DeductionWeightGrams);
                    cmd.Parameters.AddWithValue("@IsActive", bag.IsActive);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }
    }
}
