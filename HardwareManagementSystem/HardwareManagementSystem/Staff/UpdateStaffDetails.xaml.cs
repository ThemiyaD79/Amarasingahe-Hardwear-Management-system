using System;
using System.Windows;
using System.Windows.Controls;
using HardwareManagementSystem.Models;
using HardwareManagementSystem.Repositories;

namespace HardwareManagementSystem
{
    public partial class UpdateStaffWindow : Window
    {
        public StaffModel CurrentStaff { get; set; }

        public UpdateStaffWindow(StaffModel staff)
        {
            InitializeComponent();
            CurrentStaff = staff;
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

                // Set Role ComboBox correctly
                foreach (ComboBoxItem item in cmbRole.Items)
                {
                    if (item.Content.ToString() == CurrentStaff.Role)
                    {
                        cmbRole.SelectedItem = item;
                        break;
                    }
                }

                dpStartingDate.SelectedDate = CurrentStaff.StartingDate;
                chkIsActive.IsChecked = CurrentStaff.IsActive;

                // Leave password blank for security
                txtPassword.Password = string.Empty;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Only numeric validation for Salary and Age
            if (!decimal.TryParse(txtSalary.Text, out decimal salary))
            {
                MessageBox.Show("Salary must be a numeric value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtAge.Text, out int age))
            {
                MessageBox.Show("Age must be a numeric value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Update staff object
                CurrentStaff.FullName = txtFullName.Text; // optional
                CurrentStaff.Phone = txtPhone.Text;       // optional
                CurrentStaff.Username = txtUsername.Text; // optional
                CurrentStaff.NIC = txtNIC.Text;           // optional
                CurrentStaff.Salary = salary;
                CurrentStaff.Age = age;
                CurrentStaff.Role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? CurrentStaff.Role;
                CurrentStaff.StartingDate = dpStartingDate.SelectedDate ?? CurrentStaff.StartingDate;
                CurrentStaff.IsActive = chkIsActive.IsChecked ?? CurrentStaff.IsActive;

                // Update password only if typed
                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                    CurrentStaff.PasswordHash = txtPassword.Password;

                // Save to database
                StaffRepository repo = new StaffRepository();
                repo.UpdateStaff(CurrentStaff);

                MessageBox.Show("Staff details updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating staff: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}