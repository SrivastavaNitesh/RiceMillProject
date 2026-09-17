using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RiceMillProject.Models;

namespace RiceMillProject.DAL
{
    public class PersonDAL
    {
        private readonly string _connectionString;

        public PersonDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<Person> GetAllPersons()
        {
            var persons = new List<Person>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetAllPersons", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Mode", "emp");
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            persons.Add(new Person
                            {
                                PersonId = Convert.ToInt32(reader["PersonId"]),
                                PersonName = reader["PersonName"].ToString() ?? "",
                                MobileNumber = reader["MobileNumber"].ToString() ?? "",
                                Address = reader["Address"].ToString() ?? "",
                                PersonType = reader["PersonType"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return persons;
        }
        public List<Person> GetAllPersonsForcheckdublicateMobileRecord()
        {
            var persons = new List<Person>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_GetAllPersonFordublicacyMobileNumber", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            persons.Add(new Person
                            {
                                PersonId = Convert.ToInt32(reader["PersonId"]),
                                PersonName = reader["PersonName"].ToString() ?? "",
                                MobileNumber = reader["MobileNumber"].ToString() ?? "",
                                PersonType = reader["PersonType"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }
            return persons;
        }
        private DataTable ExecuteDataTable(string ProcdureName, SqlParameter[] sp)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(ProcdureName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (sp != null)
                {
                    cmd.Parameters.AddRange(sp);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;

        }
        public List<Person> GetEmployeeList(string Mode)
        {
            var persons = new List<Person>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query;
                if (Mode == "emp")
                {
                    query = @"
                        SELECT p.PersonId, p.PersonName, ISNULL(p.MobileNumber, '') AS MobileNumber, 
                               ISNULL(p.Address, '') AS Address, 
                               ISNULL(post.PostName, ISNULL(p.PersonType, 'Staff')) AS PersonType, 
                               p.IsActive
                        FROM p02_Person p
                        LEFT JOIN o10_post post ON TRY_CAST(p.PersonType AS INT) = post.PostId
                        WHERE p.PersonType NOT IN ('Kisan', 'Farmer', 'Party', 'Supplier', 'Driver', 'Worker')
                           OR post.PostId IS NOT NULL
                        ORDER BY p.PersonId DESC";
                }
                else
                {
                    query = @"
                        SELECT p.PersonId, p.PersonName, ISNULL(p.MobileNumber, '') AS MobileNumber, 
                               ISNULL(p.Address, '') AS Address, 
                               ISNULL(p.PersonType, 'Other') AS PersonType, 
                               p.IsActive
                        FROM p02_Person p
                        LEFT JOIN o10_post post ON TRY_CAST(p.PersonType AS INT) = post.PostId
                        WHERE p.PersonType IN ('Kisan', 'Farmer', 'Party', 'Supplier', 'Driver', 'Worker')
                           OR post.PostId IS NULL
                        ORDER BY p.PersonId DESC";
                }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            persons.Add(new Person
                            {
                                PersonId = Convert.ToInt32(rdr["PersonId"]),
                                PersonName = rdr["PersonName"].ToString() ?? "",
                                MobileNumber = rdr["MobileNumber"].ToString() ?? "",
                                Address = rdr["Address"].ToString() ?? "",
                                PersonType = rdr["PersonType"].ToString() ?? "",
                                IsActive = Convert.ToBoolean(rdr["IsActive"])
                            });
                        }
                    }
                }
            }
            return persons;
        }

        public bool IsMobileNumberUnique(string mobileNumber, int excludePersonId = 0)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber)) return true;
            var all = GetAllPersonsForcheckdublicateMobileRecord();
            foreach (var p in all)
            {
                if (p.PersonId != excludePersonId && p.IsActive && !string.IsNullOrWhiteSpace(p.MobileNumber) && p.MobileNumber == mobileNumber)
                {
                    return false; // Not unique
                }
            }
            return true;
        }

        public int InsertPerson(Person person)
        {
            int personId = 0;
            int designatationId = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SavePersondetails", con, trans))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PersonName", person.PersonName ?? "");
                        cmd.Parameters.AddWithValue("@MobileNumber", (object?)person.MobileNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object?)person.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PersonType", person.PersonType ?? "");
                        cmd.Parameters.AddWithValue("@O05_officeId", person.OfficeId > 0 ? person.OfficeId : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@o10postid", person.PersonType ?? "");
                        cmd.Parameters.AddWithValue("@o12_parentid", person.MethdesignatationId > 0 ? person.MethdesignatationId : (object)DBNull.Value);
                        
                        SqlParameter designationparam = new SqlParameter("@designatationId", SqlDbType.Int);
                        designationparam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(designationparam);

                        object? result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            personId = Convert.ToInt32(result);
                        }
                        if (designationparam.Value != null && designationparam.Value != DBNull.Value)
                        {
                            designatationId = Convert.ToInt32(designationparam.Value);
                        }
                    }

                    // If it's an employee (roles 1-6 or Gateman/Weightman), create login
                    var employeeRoles = new List<string> { "1", "2", "3", "4", "5", "6", "Gateman", "Weightman", "Admin", "Supervisor" };
                    if (personId > 0 && employeeRoles.Contains(person.PersonType))
                    {
                        using (SqlCommand loginCmd = new SqlCommand("sp_CreateUserLogin", con, trans))
                        {
                            loginCmd.CommandType = CommandType.StoredProcedure;
                            loginCmd.Parameters.AddWithValue("@PersonId", personId);
                            loginCmd.Parameters.AddWithValue("@MobileNumber", (object?)person.MobileNumber ?? "");
                            loginCmd.Parameters.AddWithValue("@DesignationId", designatationId);
                            loginCmd.Parameters.AddWithValue("@PersonName", person.PersonName ?? "");
                            loginCmd.Parameters.AddWithValue("@OfficeId", person.OfficeId > 0 ? person.OfficeId : 1);
                            loginCmd.Parameters.AddWithValue("@o10_postid", person.PersonType ?? "1");
                            loginCmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
            return personId;
        }

        public bool UpdatePerson(Person person)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_UpdatePerson", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PersonId", person.PersonId);
                    cmd.Parameters.AddWithValue("@PersonName", person.PersonName);
                    cmd.Parameters.AddWithValue("@MobileNumber", person.MobileNumber);
                    cmd.Parameters.AddWithValue("@Address", person.Address);
                    cmd.Parameters.AddWithValue("@PersonType", person.PersonType);
                    cmd.Parameters.AddWithValue("@IsActive", person.IsActive);
                    
                    con.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected > 0;
        }
    }
}
