using System;
using System.Data.SqlClient;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class AuthService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public Staff Authenticate(string username, string password)
        {
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                string query = "SELECT StaffID, Role, FullName FROM Staff WHERE Username = @u AND PasswordHash = @p AND IsActive = 1";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new Staff
                        {
                            StaffID = (int)r["StaffID"],
                            FullName = r["FullName"].ToString(),
                            Role = r["Role"].ToString()
                        };
                    }
                }
            }
            return null;
        }
    }
}