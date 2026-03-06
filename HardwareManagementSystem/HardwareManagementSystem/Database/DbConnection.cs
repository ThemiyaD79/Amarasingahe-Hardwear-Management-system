using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace HardwareManagementSystem.Database
{
    public static class DbConnection
    {
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
                MessageBox.Show("Database Connection Failed: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}