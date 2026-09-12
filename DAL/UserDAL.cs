using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class UserDAL
    {
        private readonly string _connectionString;

        public UserDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public User? ValidateUser(string username, string password)
        {
            User? user = null;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ValidateUser", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                Username = reader["Username"].ToString() ?? "",
                                PersonId = Convert.ToInt32(reader["PersonId"]),
                                PersonName = reader["PersonName"].ToString() ?? "",
                                o10_postid = Convert.ToInt32(reader["o10_postid"]),
                                sa10_usertypeid = Convert.ToInt32(reader["sa10_usertypeid"]),
                                Role = reader["Role"].ToString() ?? "",
                                LayoutName = reader["LayoutName"] != DBNull.Value && !string.IsNullOrEmpty(reader["LayoutName"].ToString()) ? reader["LayoutName"].ToString()! : "_Layout"
                            };
                        }
                    }
                }
            }
            return user;
        }
    }
}
