using System;
using System.Windows;
using System.Data.SqlClient;
using System.Configuration;

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
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Fetching StaffID, Role, AND FullName
                    string query = "SELECT StaffID, Role, FullName FROM Staff WHERE Username = @Username AND PasswordHash = @Password AND IsActive = 1";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int staffId = Convert.ToInt32(reader["StaffID"]);
                            string role = reader["Role"].ToString();
                            string fullName = reader["FullName"].ToString();

                            // Route them and pass their identity along!
                            if (role == "Manager" || role == "Admin")
                            {
                                MainDashboard managerDash = new MainDashboard(staffId, fullName);
                                managerDash.Show();
                                this.Close();
                            }
                            else if (role == "Cashier")
                            {
                                CashierDashboard cashierDash = new CashierDashboard(staffId, fullName);
                                cashierDash.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show($"Access Denied. The role '{role}' does not have a dashboard assigned.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invalid username or password, or your account has been disabled.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Connection Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}