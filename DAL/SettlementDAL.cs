using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class SettlementDAL
    {
        private readonly string _connectionString;

        public SettlementDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public Settlement GetSettlementDetails(string rstNumber)
        {
            Settlement settlement = new Settlement();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetSettlementDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RSTNumber", rstNumber);
                    
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settlement.RSTNumber = reader["RSTNumber"].ToString() ?? "";
                            settlement.PartyName = reader["PartyName"].ToString() ?? "";
                            settlement.VehicleNumber = reader["VehicleNumber"].ToString() ?? "";
                            settlement.GrossWeight = Convert.ToDecimal(reader["GrossWeight"]);
                            settlement.TareWeight = reader["TareWeight"] != DBNull.Value ? Convert.ToDecimal(reader["TareWeight"]) : 0;
                            settlement.NetWeight = reader["NetWeight"] != DBNull.Value ? Convert.ToDecimal(reader["NetWeight"]) : 0;
                            settlement.TotalBagDeductionKG = Convert.ToDecimal(reader["TotalBagDeductionKG"]);
                            settlement.LabDeductionPct = Convert.ToDecimal(reader["LabDeductionPct"]);
                            settlement.TotalPalledariCharges = Convert.ToDecimal(reader["TotalPalledariCharges"]);
                            settlement.Status = reader["Status"].ToString() ?? "";
                        }
                    }
                }
            }
            return settlement;
        }
    }
}
