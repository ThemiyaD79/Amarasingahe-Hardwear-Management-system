using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        // ---------------- LOAD STAFF ----------------
        private void LoadStaffData()
        {
            StaffList.Clear();

            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return;

                try
                {
                    conn.Open();
                    string query = @"SELECT StaffID, FullName, NIC, Phone, Username, PasswordHash, Role, Salary, StartingDate, Age, IsActive FROM Staff";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                StaffModel staff = new StaffModel()
                                {
                                    StaffID = Convert.ToInt32(reader["StaffID"]),
                                    FullName = reader["FullName"]?.ToString(),
                                    NIC = reader["NIC"]?.ToString(),
                                    Phone = reader["Phone"]?.ToString(),
                                    Username = reader["Username"]?.ToString(),
                                    PasswordHash = reader["PasswordHash"]?.ToString(),
                                    Role = reader["Role"]?.ToString(),
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

        // ---------------- ADD STAFF ----------------
        private void btnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            StaffDetailsWindow detailsWindow = new StaffDetailsWindow(); // Blank add mode
            detailsWindow.ShowDialog();
            LoadStaffData(); // Refresh after adding
        }

        // ---------------- UPDATE STAFF ----------------
        private void btnUpdateStaff_Click(object sender, RoutedEventArgs e)
        {
            if (dgStaff.SelectedItem == null)
            {
                MessageBox.Show("Please select a staff member from the list first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StaffModel selectedStaff = (StaffModel)dgStaff.SelectedItem;
            UpdateStaffWindow update = new UpdateStaffWindow(selectedStaff); // Pass selected staff
            update.ShowDialog();

            LoadStaffData(); // Refresh after update
        }

        // ---------------- DELETE STAFF ----------------
        private void btnDeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (dgStaff.SelectedItem == null)
            {
                MessageBox.Show("Please select a staff member from the list first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StaffModel selectedStaff = (StaffModel)dgStaff.SelectedItem;

            var confirm = MessageBox.Show($"Are you sure you want to delete {selectedStaff.FullName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return;

                try
                {
                    conn.Open();
                    string query = "DELETE FROM Staff WHERE StaffID=@StaffID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StaffID", selectedStaff.StaffID);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Staff deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadStaffData(); // Refresh
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting staff: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ---------------- SEARCH STAFF (FULL NAME ONLY) ----------------
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            FilterStaffByFullName();
        }

        // ---------------- VIEW ALL STAFF ----------------
        private void btnViewAll_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";             // Clear search box
            dgStaff.ItemsSource = StaffList; // Reset DataGrid to full list
        }

        // ---------------- HELPER METHOD: FILTER STAFF ----------------
        private void FilterStaffByFullName()
        {
            string searchText = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                dgStaff.ItemsSource = StaffList;
                return;
            }

            var filtered = StaffList
                .Where(s => !string.IsNullOrEmpty(s.FullName) &&
                            s.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            dgStaff.ItemsSource = new ObservableCollection<StaffModel>(filtered);
        }

        // ---------------- OPTIONAL: LIVE SEARCH ----------------
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterStaffByFullName();
        }



        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainDashboard dashboard = new MainDashboard();
            dashboard.Show();
            this.Close(); // Close the StaffManagement window
        }

    }
}