using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HardwareManagementSystem.Database;
using HardwareManagementSystem.Models;
using HardwareManagementSystem.Inventory;

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
            LoadCategories();

            cmbCategoryFilter.SelectionChanged += cmbCategoryFilter_SelectionChanged;
        }

        // --- LOAD ALL PRODUCTS ---
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

        // --- LOAD CATEGORIES ---
        private void LoadCategories()
        {
            cmbCategoryFilter.Items.Clear();
            cmbCategoryFilter.Items.Add("All"); // Option to view all items
            cmbCategoryFilter.Items.Add("Tools");
            cmbCategoryFilter.Items.Add("Electrical");
            cmbCategoryFilter.Items.Add("Plumbing");
            cmbCategoryFilter.Items.Add("Paint");
            cmbCategoryFilter.Items.Add("Hardware");

            cmbCategoryFilter.SelectedIndex = 0; // Default to All
        }

        // --- CATEGORY SELECTION CHANGED ---
        private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedCategory = cmbCategoryFilter.SelectedItem?.ToString();

            if (selectedCategory == "All")
            {
                dgInventory.ItemsSource = ProductsList;
            }
            else
            {
                dgInventory.ItemsSource = CategoryFilter.FilterByCategory(ProductsList, selectedCategory);
            }
        }

        // --- ADD ITEM ---
        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            ItemDetailsWindow detailsWindow = new ItemDetailsWindow();
            if (detailsWindow.ShowDialog() == true)
            {
                LoadProductData();
            }
        }

        // --- UPDATE ITEM ---
        private void btnUpdateItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgInventory.SelectedItem is ProductModel selected)
            {
                UpdateInventory updateWindow = new UpdateInventory(selected);
                if (updateWindow.ShowDialog() == true)
                {
                    LoadProductData(); // Refresh DataGrid after update
                }
            }
            else
            {
                MessageBox.Show("Please select an item to update.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- DELETE ITEM ---
        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgInventory.SelectedItem is ProductModel selected)
            {
                var result = MessageBox.Show($"Are you sure you want to delete '{selected.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteProduct(selected.ProductID);
                    LoadProductData();
                }
            }
            else
            {
                MessageBox.Show("Please select an item to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteProduct(int productId)
        {
            try
            {
                using (SqlConnection conn = DbConnection.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM Product WHERE ProductID=@ProductID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Item deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting item: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- SEARCH ITEMS ---
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearch.Text;
            string categoryFilter = cmbCategoryFilter.SelectedItem?.ToString();

            var filteredProducts = Search.FilterProducts(ProductsList, searchText, categoryFilter);
            dgInventory.ItemsSource = filteredProducts;
        }

        // --- VIEW ALL BUTTON ---
        private void btnViewAll_Click(object sender, RoutedEventArgs e)
        {
            dgInventory.ItemsSource = ProductsList;
            cmbCategoryFilter.SelectedIndex = 0;
            txtSearch.Clear();
        }
    }

    // --- CATEGORY FILTER CLASS ---
    public static class CategoryFilter
    {
        public static IEnumerable<ProductModel> FilterByCategory(IEnumerable<ProductModel> products, string category)
        {
            return products.Where(p => p.Category == category).ToList();
        }
    }

    // --- SEARCH CLASS ---
    public static class Search
    {
        public static IEnumerable<ProductModel> FilterProducts(IEnumerable<ProductModel> products, string searchText, string category)
        {
            var result = products;

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                result = result.Where(p => p.Category == category);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                // Fixed case-insensitive search using IndexOf
                result = result.Where(p =>
                    p.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Brand.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return result.ToList();
        }
    }
}