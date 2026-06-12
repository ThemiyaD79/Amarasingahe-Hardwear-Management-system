using System;
using System.Data.SQLite;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class AuthService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public Staff Authenticate(string username, string password)
        {
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                con.Open();
                string query = "SELECT StaffID, Role, FullName FROM Staff WHERE Username = @u AND PasswordHash = @p AND IsActive = 1";
                SQLiteCommand cmd = new SQLiteCommand(query, con);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);

                using (SQLiteDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new Staff
                        {
                            StaffID = Convert.ToInt32(r["StaffID"]),
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