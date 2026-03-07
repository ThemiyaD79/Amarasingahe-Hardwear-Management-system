using System;
using System.Data.SqlClient;
using System.Windows;
using HardwareManagementSystem.Database;

namespace HardwareManagementSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim(); // WPF uses ..Password for security!

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both a Username and a Password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return;

                try
                {
                    conn.Open();
                    string query = "SELECT Role, IsActive FROM Staff WHERE Username = @username AND PasswordHash = @password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool isActive = Convert.ToBoolean(reader["IsActive"]);
                                if (!isActive)
                                {
                                    MessageBox.Show("Your account is disabled. Please contact the Admin.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                string role = reader["Role"].ToString();

                                // === NEW NAVIGATION LOGIC ===
                                // 1. Create the Main Dashboard Window
                                MainDashboard dashboard = new MainDashboard();

                                // 2. Open the Dashboard
                                dashboard.Show();

                                // 3. Close the Login Window so it doesn't stay open in the background
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Invalid Username or Password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}