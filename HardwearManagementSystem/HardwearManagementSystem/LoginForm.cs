using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using HardwearManagementSystem.Database; // This lets us use your DbConnection class!

namespace HardwearManagementSystem
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 1. Grab what the user typed
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            // 2. Check if the boxes are empty before bothering the database
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both a Username and a Password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Open the connection using your helper class
            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return; // If connection fails, stop here.

                try
                {
                    conn.Open();

                    // 4. Write the SQL Query (Using @ parameters for top-tier security)
                    string query = "SELECT Role, IsActive FROM Staff WHERE Username = @username AND PasswordHash = @password";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Safely inject the text box values into the query
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) // If it finds a match in the database
                            {
                                bool isActive = Convert.ToBoolean(reader["IsActive"]);
                                
                                // Check if they were fired or disabled
                                if (!isActive)
                                {
                                    MessageBox.Show("Your account is disabled. Please contact the Admin.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                // Login Success! Grab their role (Manager/Cashier)
                                string role = reader["Role"].ToString();
                                MessageBox.Show($"Welcome to Amarasinghe Hardware!\nLogged in as: {role}", "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                // We will add the code to open the main dashboard here later!
                            }
                            else
                            {
                                MessageBox.Show("Invalid Username or Password. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}