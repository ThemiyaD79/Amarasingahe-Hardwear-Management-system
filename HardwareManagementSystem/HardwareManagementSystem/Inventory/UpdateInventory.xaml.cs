using System;
using System.Windows;
using HardwareManagementSystem.Models;
using HardwareManagementSystem.Repositories;

namespace HardwareManagementSystem.Inventory
{
    public partial class UpdateInventory : Window
    {
        private ProductModel selectedProduct;
        private ProductRepository repo;

        public UpdateInventory(ProductModel product)
        {
            InitializeComponent();
            selectedProduct = product;
            repo = new ProductRepository();

            LoadCategories();
            PrefillFields();
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Add("Tools");
            cmbCategory.Items.Add("Electrical");
            cmbCategory.Items.Add("Plumbing");
            cmbCategory.Items.Add("Paint");
            cmbCategory.Items.Add("Hardware");
        }

        private void PrefillFields()
        {
            txtItemName.Text = selectedProduct.Name;
            cmbCategory.Text = selectedProduct.Category;
            txtBrand.Text = selectedProduct.Brand;
            txtQuantity.Text = selectedProduct.TotalQuantity.ToString();
            txtPrice.Text = selectedProduct.CurrentSellingPrice.ToString();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtItemName.Text == "" || cmbCategory.Text == "" || txtBrand.Text == "" || txtQuantity.Text == "" || txtPrice.Text == "")
                {
                    MessageBox.Show("Please fill all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string confirmMessage = $"Confirm update?\n\n" +
                    $"Name: {txtItemName.Text}\n" +
                    $"Category: {cmbCategory.Text}\n" +
                    $"Brand: {txtBrand.Text}\n" +
                    $"Quantity: {txtQuantity.Text}\n" +
                    $"Price: {txtPrice.Text}";

                if (MessageBox.Show(confirmMessage, "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                // Create updated ProductModel
                selectedProduct.Name = txtItemName.Text;
                selectedProduct.Category = cmbCategory.Text;
                selectedProduct.Brand = txtBrand.Text;
                selectedProduct.TotalQuantity = Convert.ToInt32(txtQuantity.Text);
                selectedProduct.CurrentSellingPrice = Convert.ToDecimal(txtPrice.Text);

                // Call repository to update DB
                repo.UpdateProduct(selectedProduct);

                MessageBox.Show("Item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating item: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}