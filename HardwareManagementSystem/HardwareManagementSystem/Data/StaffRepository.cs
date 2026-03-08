using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using HardwareManagementSystem.Models; // must match StaffModel namespace

namespace HardwareManagementSystem.Repositories
{
    public class StaffRepository
    {
        string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public void AddStaff(StaffModel staff)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"INSERT INTO Staff
                (FullName, NIC, Phone, Username, PasswordHash, Role, Salary, StartingDate, Age, IsActive)
                VALUES
                (@FullName, @NIC, @Phone, @Username, @PasswordHash, @Role, @Salary, @StartingDate, @Age, @IsActive)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", staff.FullName);
                cmd.Parameters.AddWithValue("@NIC", staff.NIC);
                cmd.Parameters.AddWithValue("@Phone", staff.Phone);
                cmd.Parameters.AddWithValue("@Username", staff.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", staff.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", staff.Role);
                cmd.Parameters.AddWithValue("@Salary", staff.Salary);
                cmd.Parameters.AddWithValue("@StartingDate", staff.StartingDate);
                cmd.Parameters.AddWithValue("@Age", staff.Age);
                cmd.Parameters.AddWithValue("@IsActive", staff.IsActive);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<StaffModel> GetAllStaff()
        {
            List<StaffModel> staffList = new List<StaffModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Staff";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    staffList.Add(new StaffModel
                    {
                        StaffID = (int)reader["StaffID"],
                        FullName = reader["FullName"].ToString(),
                        NIC = reader["NIC"].ToString(),
                        Phone = reader["Phone"].ToString(),
                        Username = reader["Username"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Role = reader["Role"].ToString(),
                        Salary = Convert.ToDecimal(reader["Salary"]),
                        StartingDate = Convert.ToDateTime(reader["StartingDate"]),
                        Age = (int)reader["Age"],
                        IsActive = (bool)reader["IsActive"]
                    });
                }
            }

            return staffList;
        }


        public void UpdateStaff(StaffModel staff)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"UPDATE Staff SET 
                        FullName=@FullName, NIC=@NIC, Phone=@Phone, Username=@Username,
                        PasswordHash=@PasswordHash, Role=@Role, Salary=@Salary, 
                        StartingDate=@StartingDate, Age=@Age, IsActive=@IsActive
                        WHERE StaffID=@StaffID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", staff.FullName);
                cmd.Parameters.AddWithValue("@NIC", staff.NIC);
                cmd.Parameters.AddWithValue("@Phone", staff.Phone);
                cmd.Parameters.AddWithValue("@Username", staff.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", staff.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", staff.Role);
                cmd.Parameters.AddWithValue("@Salary", staff.Salary);
                cmd.Parameters.AddWithValue("@StartingDate", staff.StartingDate);
                cmd.Parameters.AddWithValue("@Age", staff.Age);
                cmd.Parameters.AddWithValue("@IsActive", staff.IsActive);
                cmd.Parameters.AddWithValue("@StaffID", staff.StaffID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}