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
                                OfficeId = HasColumn(reader, "OfficeId") && reader["OfficeId"] != DBNull.Value
                                    ? Convert.ToInt32(reader["OfficeId"])
                                    : 0,
                                Role = reader["Role"].ToString() ?? "",
                                LayoutName = reader["LayoutName"] != DBNull.Value && !string.IsNullOrEmpty(reader["LayoutName"].ToString()) ? reader["LayoutName"].ToString()! : "_Layout"
                            };
                        }
                    }

                    if (user != null && user.OfficeId <= 0)
                    {
                        using var officeColumnCmd = new SqlCommand(@"
                            SELECT TOP (1) COLUMN_NAME
                            FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'sa05_user'
                              AND LOWER(REPLACE(COLUMN_NAME, '_', '')) IN ('officeid', 'o05officeid')
                            ORDER BY CASE WHEN COLUMN_NAME = 'OfficeId' THEN 0 ELSE 1 END;", con);
                        var officeColumn = Convert.ToString(officeColumnCmd.ExecuteScalar());
                        if (!string.IsNullOrWhiteSpace(officeColumn))
                        {
                            using var officeCmd = new SqlCommand(
                                $"SELECT TOP (1) [{officeColumn.Replace("]", "]]", StringComparison.Ordinal)}] FROM sa05_user WHERE UserId = @UserId", con);
                            officeCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                            var officeId = officeCmd.ExecuteScalar();
                            if (officeId != null && officeId != DBNull.Value)
                                user.OfficeId = Convert.ToInt32(officeId);
                        }
                    }
                }
            }
            return user;
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
