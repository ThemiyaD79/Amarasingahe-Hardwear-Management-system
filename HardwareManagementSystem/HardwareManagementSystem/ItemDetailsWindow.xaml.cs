using System.Windows;

namespace HardwareManagementSystem
{
    public partial class ItemDetailsWindow : Window
    {
        public ItemDetailsWindow()
        {
            InitializeComponent();
        }

        // This makes the gray Cancel button work!
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}