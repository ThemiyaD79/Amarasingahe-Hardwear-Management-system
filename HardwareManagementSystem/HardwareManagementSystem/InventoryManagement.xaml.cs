using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows; // This fixes the RoutedEventArgs error!
using HardwareManagementSystem.Database;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem
{
    public partial class InventoryManagement : Window
    {
        public ObservableCollection<ProductModel> ProductsList { get; set; }

        public InventoryManagement()
        {
            InitializeComponent();

            this.DataContext = this;
            ProductsList = new ObservableCollection<ProductModel>();

            LoadProductData();
        }

        private void LoadProductData()
        {
            ProductsList.Clear();

            using (SqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return;

                try
                {
                    conn.Open();
                    string query = "SELECT ProductID, Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID FROM Product";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ProductModel product = new ProductModel()
                                {
                                    ProductID = Convert.ToInt32(reader["ProductID"]),
                                    Name = reader["Name"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    Brand = reader["Brand"].ToString(),
                                    CurrentSellingPrice = Convert.ToDecimal(reader["CurrentSellingPrice"]),
                                    TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                                    ReorderLevel = Convert.ToInt32(reader["ReorderLevel"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    StaffID = Convert.ToInt32(reader["StaffID"])
                                };

                                ProductsList.Add(product);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading inventory data: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            dgInventory.ItemsSource = ProductsList;
        }

        // --- BUTTON LOGIC ---

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            // Opens the blank popup
            ItemDetailsWindow detailsWindow = new ItemDetailsWindow();
            detailsWindow.ShowDialog();
        }

        private void btnUpdateItem_Click(object sender, RoutedEventArgs e)
        {
            // Opens the popup for updating
            ItemDetailsWindow detailsWindow = new ItemDetailsWindow();
            detailsWindow.ShowDialog();
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            // Shows the standard Windows warning popup
            MessageBox.Show("Are you sure you want to delete this item?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            // Search logic will go here later
        }
    }
}