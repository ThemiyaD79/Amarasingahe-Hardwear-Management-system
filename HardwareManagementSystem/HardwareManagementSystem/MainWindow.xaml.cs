using System;
using System.Windows;

namespace HardwareManagementSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow() { InitializeComponent(); }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            AuthService auth = new AuthService();
            Staff user = auth.Authenticate(txtUsername.Text, txtPassword.Password);

            if (user != null)
            {
                if (user.Role == "Manager" || user.Role == "Admin")
                {
                    MainDashboard md = new MainDashboard(user.StaffID, user.FullName);
                    md.Show();
                }
                else
                {
                    CashierDashboard cd = new CashierDashboard(user.StaffID, user.FullName);
                    cd.Show();
                }
                this.Close();
            }
            else MessageBox.Show("Invalid Login.");
        }
    }
}