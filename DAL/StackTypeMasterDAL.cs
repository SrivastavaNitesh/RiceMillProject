using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class StackTypeMasterDAL
    {
        private readonly string _connectionString;

        public StackTypeMasterDAL(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection") ?? "";
        }


        public List<StackTypeMaster> GetActiveStackTypes()
        {
            var list = new List<StackTypeMaster>();

            using (SqlConnection con =
                   new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd =
                       new SqlCommand(
                           "sp_GetActiveStackTypes",
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
                            list.Add(
                                new StackTypeMaster
                                {
                                    StackTypeId =
                                        Convert.ToInt32(
                                            reader["StackTypeId"]
                                        ),

                                    StackTypeName =
                                        reader["StackTypeName"]
                                        ?.ToString() ?? "",

                                    SortOrder =
                                        Convert.ToInt32(
                                            reader["SortOrder"]
                                        ),

                                    IsActive =
                                        Convert.ToBoolean(
                                            reader["IsActive"]
                                        ),

                                    CreatedAt =
                                        Convert.ToDateTime(
                                            reader["CreatedAt"]
                                        )
                                }
                            );
                        }
                    }
                }
            }

            return list;
        }
    }
}