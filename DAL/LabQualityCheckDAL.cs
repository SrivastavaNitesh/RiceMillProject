using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class LabQualityCheckDAL
    {
        private readonly string _connectionString;

        public LabQualityCheckDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public bool SaveLabQualityCheck(LabQualityCheck labCheck)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_SaveLabQualityCheck", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RSTNumber", labCheck.RSTNumber);
                    cmd.Parameters.AddWithValue("@MoisturePct", labCheck.MoisturePct);
                    cmd.Parameters.AddWithValue("@DustPct", labCheck.DustPct);
                    cmd.Parameters.AddWithValue("@PayiaPct", labCheck.PayiaPct);
                    cmd.Parameters.AddWithValue("@GrainQualityPct", labCheck.GrainQualityPct);
                    cmd.Parameters.AddWithValue("@TotalDeductionPct", labCheck.TotalDeductionPct);
                    cmd.Parameters.AddWithValue("@TestedBy", labCheck.TestedBy);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }
    }
}
