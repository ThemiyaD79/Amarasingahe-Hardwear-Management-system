using System;
using System.Text.RegularExpressions;
using System.Windows;
using HardwareManagementSystem.Models;
using HardwareManagementSystem.Repositories;

namespace HardwareManagementSystem
{
    public partial class StaffDetailsWindow : Window
    {
        public StaffModel CurrentStaff { get; set; }

        public StaffDetailsWindow()
        {
            InitializeComponent();
        }

        public StaffDetailsWindow(StaffModel selectedStaff)
        {
            InitializeComponent();
            CurrentStaff = selectedStaff;
            PreFillData();
        }

        private void PreFillData()
        {
            if (CurrentStaff != null)
            {
                txtFullName.Text = CurrentStaff.FullName;
                txtPhone.Text = CurrentStaff.Phone;
                txtUsername.Text = CurrentStaff.Username;
                txtSalary.Text = CurrentStaff.Salary.ToString("0.00");
                txtAge.Text = CurrentStaff.Age.ToString();
                txtNIC.Text = CurrentStaff.NIC;
                cmbRole.Text = CurrentStaff.Role;
                dpStartingDate.SelectedDate = CurrentStaff.StartingDate;
                chkIsActive.IsChecked = CurrentStaff.IsActive;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                string.IsNullOrWhiteSpace(txtNIC.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Password) ||
                string.IsNullOrWhiteSpace(txtSalary.Text) ||
                string.IsNullOrWhiteSpace(txtAge.Text) ||
                cmbRole.SelectedIndex < 0 ||
                dpStartingDate.SelectedDate == null)
            {
                MessageBox.Show("Please fill in all fields!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            if (!decimal.TryParse(txtSalary.Text, out decimal salary))
            {
                MessageBox.Show("Salary must be a valid number!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtAge.Text, out int age))
            {
                MessageBox.Show("Age must be a valid number!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StaffModel staff = new StaffModel
            {
                FullName = txtFullName.Text,
                NIC = txtNIC.Text,
                Phone = txtPhone.Text,
                Username = txtUsername.Text,
                PasswordHash = txtPassword.Password,
                Role = cmbRole.Text,
                Salary = salary,
                StartingDate = dpStartingDate.SelectedDate.Value,
                Age = age,
                IsActive = chkIsActive.IsChecked ?? false
            };

            // Confirm before adding/updating
            var confirm = MessageBox.Show(
                $"Please confirm the details:\n\n" +
                $"Full Name: {staff.FullName}\n" +
                $"NIC: {staff.NIC}\n" +
                $"Phone: {staff.Phone}\n" +
                $"Username: {staff.Username}\n" +
                $"Role: {staff.Role}\n" +
                $"Salary: {staff.Salary}\n" +
                $"Age: {staff.Age}\n" +
                $"Starting Date: {staff.StartingDate:yyyy-MM-dd}\n" +
                $"Active: {staff.IsActive}",
                "Confirm Details",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                StaffRepository repo = new StaffRepository();

                if (CurrentStaff == null)
                {
                    // ADD
                    repo.AddStaff(staff);
                    MessageBox.Show("Staff added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // TODO: Implement update logic
                    MessageBox.Show("Staff updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving staff: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}