using System;
using System.Windows;

namespace HardwareManagementSystem
{
    public partial class CashierDashboard : Window
    {
        public CashierDashboard()
        {
            InitializeComponent();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Opens the login window again
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();

            // Closes the current dashboard
            this.Close();
        }
    }
}