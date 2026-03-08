using System.Windows;

namespace HardwareManagementSystem
{
    public partial class MainDashboard : Window
    {
        // 1. The Constructor (Best practice is to keep this at the very top!)
        public MainDashboard()
        {
            InitializeComponent();
        }

        // 2. This wires up the Staff Management Button
        private void btnStaffNav_Click(object sender, RoutedEventArgs e)
        {
            StaffManagement staffWindow = new StaffManagement();
            staffWindow.Show();
        }

        // 3. This wires up the Inventory Button
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e)
        {
            InventoryManagement invWindow = new InventoryManagement();
            invWindow.Show();
        }

        // 4. This wires up the NEW Cashier Dashboard Button
        private void btnCashierNav_Click(object sender, RoutedEventArgs e)
        {
            // Note: This assumes you have created a WPF Window named CashierDashboard.xaml!
            CashierDashboard cashierWindow = new CashierDashboard();
            cashierWindow.Show();
        }

        // 5. This wires up the Logout Button
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();

            this.Close(); // Closes the dashboard
        }
    }
}