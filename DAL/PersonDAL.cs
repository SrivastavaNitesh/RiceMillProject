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

            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] sp = new SqlParameter[2];
                sp[0] = new SqlParameter("@officeid", "0");
                sp[1] = new SqlParameter("@Mode", Mode);
                dt = ExecuteDataTable("sp_GetAllPersons", sp);
                if (dt!=null && dt.Rows.Count>0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        persons.Add(new Person
                        {
                            PersonId = Convert.ToInt32(dt.Rows[i]["PersonId"]),
                            PersonName = dt.Rows[i]["PersonName"].ToString() ?? "",
                            MobileNumber = dt.Rows[i]["MobileNumber"].ToString() ?? "",
                            Address = dt.Rows[i]["Address"].ToString() ?? "",
                            PersonType = dt.Rows[i]["PersonType"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dt.Rows[i]["IsActive"])
                        });
                    }
                }
            }
            catch (Exception e)
            {
                dt = null;
            }
            return persons;
        }

        public bool IsMobileNumberUnique(string mobileNumber, int excludePersonId = 0)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber)) return false;
            var all = GetAllPersonsForcheckdublicateMobileRecord();
            foreach (var p in all)
            {
                if (p.PersonId != excludePersonId && p.IsActive && p.MobileNumber == mobileNumber)
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
                {
                    try
                    {

                        using (SqlCommand cmd = new SqlCommand("sp_SavePersondetails", con, trans))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@PersonName", person.PersonName);
                            cmd.Parameters.AddWithValue("@MobileNumber", person.MobileNumber ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Address", person.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PersonType", person.PersonType);
                            cmd.Parameters.AddWithValue("@O05_officeId", person.OfficeId);
                            cmd.Parameters.AddWithValue("@o10postid", person.PersonType);
                            cmd.Parameters.AddWithValue("@o12_parentid", person.MethdesignatationId);
                            SqlParameter designationparamm= cmd.Parameters.AddWithValue("@designatationId", SqlDbType.Int);
                            designationparamm.Direction = ParameterDirection.Output;
                            //con.Open();
                            object? result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                personId = Convert.ToInt32(result);
                                designatationId = Convert.ToInt32(designationparamm.Value);
                            }
                        }

                        // If it's an employee (not Kisan, Driver, Worker, Center, Party) create login
                        var employeeRoles = new List<string> { "4", "1", "2", "3", "4", "5", "6" };
                        if (personId > 0 && employeeRoles.Contains(person.PersonType))
                        {
                            using (SqlCommand loginCmd = new SqlCommand("sp_CreateUserLogin", con, trans))
                            {
                                loginCmd.CommandType = CommandType.StoredProcedure;
                                loginCmd.Parameters.AddWithValue("@PersonId", personId);
                                loginCmd.Parameters.AddWithValue("@MobileNumber", person.MobileNumber ?? "");
                                loginCmd.Parameters.AddWithValue("@designatationId", designatationId);
                                loginCmd.Parameters.AddWithValue("@PersonName", person.PersonName);
                                loginCmd.Parameters.AddWithValue("@O05_OfficeId", person.OfficeId);
                                loginCmd.Parameters.AddWithValue("@O10_PostId", person.PersonType);
                                loginCmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                    }
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
