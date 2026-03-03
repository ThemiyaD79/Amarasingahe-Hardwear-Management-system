using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HardwareManagementSystem.Database
{
    public static class DbConnection
    {
        // This reaches into your App.config and grabs the string safely!
        private static string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            try
            {
                SqlConnection conn = new SqlConnection(connectionString);
                return conn;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Connection Failed: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}