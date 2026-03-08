using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace HardwareManagementSystem.Database
{
    public static class DbConnection
    {
        public static SqlConnection GetConnection()
        {
            try
            {
                // This magically grabs the exact string from your App.config file!
                string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

                SqlConnection conn = new SqlConnection(connectionString);
                return conn;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read database connection: " + ex.Message, "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}