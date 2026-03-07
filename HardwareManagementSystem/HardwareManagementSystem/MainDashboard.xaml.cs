using System.Windows;

namespace HardwareManagementSystem
{
    public partial class MainDashboard : Window
    {
        public MainDashboard()
        {
            InitializeComponent();
        }

        // 1. This wires up the Inventory Button!
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e)
        {
            InventoryManagement invWindow = new InventoryManagement();
            invWindow.Show();
        }

        // 2. This wires up the Logout Button!
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();

            this.Close(); // Closes the dashboard
        }
    }
}