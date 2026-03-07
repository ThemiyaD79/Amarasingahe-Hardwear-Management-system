using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using HardwareManagementSystem.Database;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem
{
    public partial class StaffManagement : Window
    {
        public ObservableCollection<StaffModel> StaffList { get; set; }

        public StaffManagement()
        {
            InitializeComponent();
            this.DataContext = this;
            StaffList = new ObservableCollection<StaffModel>();

            LoadStaffData();
        }

        private void LoadStaffData()
        {
            StaffList.Clear();

            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return;

                try
                {
                    conn.Open();
                    string query = "SELECT StaffID, FullName, NIC, Phone, Username, PasswordHash, Role, Salary, StartingDate, Age, IsActive FROM Staff";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                StaffModel staff = new StaffModel()
                                {
                                    StaffID = Convert.ToInt32(reader["StaffID"]),
                                    FullName = reader["FullName"].ToString(),
                                    NIC = reader["NIC"].ToString(),
                                    Phone = reader["Phone"].ToString(),
                                    Username = reader["Username"].ToString(),
                                    PasswordHash = reader["PasswordHash"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    Salary = Convert.ToDecimal(reader["Salary"]),
                                    StartingDate = Convert.ToDateTime(reader["StartingDate"]),
                                    Age = Convert.ToInt32(reader["Age"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])
                                };
                                StaffList.Add(staff);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading staff data: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            dgStaff.ItemsSource = StaffList;
        }

        // --- BUTTON LOGIC ---

        private void btnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            // Opens the window in "Add Mode" (Blank)
            StaffDetailsWindow detailsWindow = new StaffDetailsWindow();
            detailsWindow.ShowDialog();
        }

        private void btnUpdateStaff_Click(object sender, RoutedEventArgs e)
        {
            // 1. Make sure the user actually clicked a row before hitting update
            if (dgStaff.SelectedItem == null)
            {
                MessageBox.Show("Please select a staff member from the list first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Grab the selected row and convert it into our StaffModel
            StaffModel selectedStaff = (StaffModel)dgStaff.SelectedItem;

            // 3. Open the pop-up, and PASS the data into "Update Mode"!
            StaffDetailsWindow detailsWindow = new StaffDetailsWindow(selectedStaff);
            detailsWindow.ShowDialog();
        }

        private void btnDeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            // Delete logic will go here
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            // Search logic will go here
        }
    }
}