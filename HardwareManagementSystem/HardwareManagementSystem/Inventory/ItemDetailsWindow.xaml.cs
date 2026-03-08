using System;
using System.Data.SqlClient;
using System.Windows;
using HardwareManagementSystem.Database;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem
{
    public partial class ItemDetailsWindow : Window
    {
        private ProductModel selectedProduct; // If not null → update mode

        public ItemDetailsWindow()
        {
            InitializeComponent();
            LoadCategories();
        }

        // Overloaded constructor for Update
        public ItemDetailsWindow(ProductModel product)
        {
            InitializeComponent();
            LoadCategories();
            selectedProduct = product;

            // Prefill fields
            txtItemName.Text = product.Name;
            cmbCategory.Text = product.Category;
            Brand.Text = product.Brand;
            txtQuantity.Text = product.TotalQuantity.ToString();
            txtPrice.Text = product.CurrentSellingPrice.ToString();
        }

        // Load categories into ComboBox
        private void LoadCategories()
        {
            cmbCategory.Items.Add("Tools");
            cmbCategory.Items.Add("Electrical");
            cmbCategory.Items.Add("Plumbing");
            cmbCategory.Items.Add("Paint");
            cmbCategory.Items.Add("Hardware");
        }

        // SAVE BUTTON
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (txtItemName.Text == "" ||
                    cmbCategory.Text == "" ||
                    Brand.Text == "" ||
                    txtQuantity.Text == "" ||
                    txtPrice.Text == "")
                {
                    MessageBox.Show("Please fill all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirmation message
                string confirmationMessage =
                    "Please confirm the item details:\n\n" +
                    "Item Name: " + txtItemName.Text + "\n" +
                    "Category: " + cmbCategory.Text + "\n" +
                    "Brand: " + Brand.Text + "\n" +
                    "Quantity: " + txtQuantity.Text + "\n" +
                    "Unit Price (Rs): " + txtPrice.Text + "\n\n" +
                    "Are these details correct?";

                MessageBoxResult result = MessageBox.Show(
                    confirmationMessage,
                    "Confirm Item Details",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                using (SqlConnection conn = DbConnection.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd;

                    if (selectedProduct == null)
                    {
                        // INSERT new product
                        string query = @"INSERT INTO Product
                                        (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID)
                                        VALUES
                                        (@Name,@Category,@Brand,@Price,@Quantity,5,1,1)";

                        cmd = new SqlCommand(query, conn);
                    }
                    else
                    {
                        // UPDATE existing product
                        string query = @"UPDATE Product SET
                                         Name=@Name,
                                         Category=@Category,
                                         Brand=@Brand,
                                         CurrentSellingPrice=@Price,
                                         TotalQuantity=@Quantity
                                         WHERE ProductID=@ProductID";

                        cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                    }

                    // Common parameters
                    cmd.Parameters.AddWithValue("@Name", txtItemName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Brand", Brand.Text);
                    cmd.Parameters.AddWithValue("@Price", Convert.ToDecimal(txtPrice.Text));
                    cmd.Parameters.AddWithValue("@Quantity", Convert.ToInt32(txtQuantity.Text));

                    cmd.ExecuteNonQuery();
                }

                // Success message
                string successMessage = selectedProduct == null ? "Item successfully added!" : "Item successfully updated!";
                MessageBox.Show(successMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close window
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving item: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // CANCEL BUTTON
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}