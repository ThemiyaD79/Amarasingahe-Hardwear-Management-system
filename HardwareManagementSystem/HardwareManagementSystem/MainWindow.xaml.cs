using System;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;

namespace HardwareManagementSystem
{
    public partial class MainWindow : Window
    {
        private bool isUpdatingPassword = false;

        public MainWindow() { InitializeComponent(); }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void chkShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdatingPassword)
            {
                isUpdatingPassword = true;
                txtPasswordVisible.Text = txtPassword.Password;
                isUpdatingPassword = false;
            }
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdatingPassword)
            {
                isUpdatingPassword = true;
                txtPassword.Password = txtPasswordVisible.Text;
                isUpdatingPassword = false;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            AuthService auth = new AuthService();

            Staff user = auth.Authenticate(txtUsername.Text, txtPassword.Password);

            if (user != null)
            {
                if (user.Role == "Manager" || user.Role == "Admin")
                {
                    MainDashboard md = new MainDashboard(user.StaffID, user.FullName);
                    md.WindowState = this.WindowState;
                    if (this.WindowState == WindowState.Maximized) md.WindowStartupLocation = WindowStartupLocation.Manual;
                    md.Show();
                }
                else
                {
                    CashierDashboard cd = new CashierDashboard(user.StaffID, user.FullName);
                    cd.WindowState = this.WindowState;
                    if (this.WindowState == WindowState.Maximized) cd.WindowStartupLocation = WindowStartupLocation.Manual;
                    cd.Show();
                }
                this.Close();
            }
            else MessageBox.Show("Invalid Login.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}