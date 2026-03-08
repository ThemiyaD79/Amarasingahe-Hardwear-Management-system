using System.Windows;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem
{
    public partial class StaffDetailsWindow : Window
    {
        // This holds the data if we are in "Update" mode
        public StaffModel CurrentStaff { get; set; }

        // MODE 1: ADDING (This runs when you click "+ Add New Staff")
        public StaffDetailsWindow()
        {
            InitializeComponent();
        }

        // MODE 2: UPDATING (This runs when you click "Update Selected" and pass it data)
        public StaffDetailsWindow(StaffModel selectedStaff)
        {
            InitializeComponent();
            CurrentStaff = selectedStaff;

            // Push the data into the UI!
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

                // Notice we leave Password blank for security. If they type a new one, it overrides.
            }
        }

        // A quick wire-up for your Cancel button so the window closes properly
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}