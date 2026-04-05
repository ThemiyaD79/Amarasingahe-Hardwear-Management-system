using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HardwareManagementSystem
{
    public partial class CashierDashboard : Window
    {
        public ObservableCollection<CartItem> ShoppingCart { get; set; }
        private ObservableCollection<CashierProductItem> AllProducts { get; set; }
        private decimal currentGrandTotal = 0;

        // Identity Tracking
        private int loggedInStaffID;
        private string loggedInStaffName;

        public CashierDashboard(int staffId = 4, string staffName = "Cashier")
        {
            InitializeComponent();
            loggedInStaffID = staffId;
            loggedInStaffName = staffName;

            // Set the Cashier's real name in the UI
            if (TxtCurrentUserName != null)
                TxtCurrentUserName.Text = loggedInStaffName;

            ShoppingCart = new ObservableCollection<CartItem>();
            AllProducts = new ObservableCollection<CashierProductItem>();
            DgdCart.ItemsSource = ShoppingCart;
            LoadProductsFromDatabase();
        }

        private void LoadProductsFromDatabase()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Groups by product to calculate live stock from the Batch table, and fetches the ImagePath
                    string query = @"
                        SELECT p.ProductID, p.Name, p.CurrentSellingPrice, p.Category, p.ImagePath, 
                               ISNULL(SUM(b.CurrentQuantity), 0) AS StockLeft 
                        FROM Product p 
                        LEFT JOIN Batch b ON p.ProductID = b.ProductID 
                        WHERE p.IsActive = 1 
                        GROUP BY p.ProductID, p.Name, p.CurrentSellingPrice, p.Category, p.ImagePath";

                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        AllProducts.Add(new CashierProductItem
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["Name"].ToString(),
                            SellingPrice = Convert.ToDecimal(reader["CurrentSellingPrice"]),
                            Category = reader["Category"].ToString(),
                            StockLeft = Convert.ToInt32(reader["StockLeft"]),
                            ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : null
                        });
                    }
                    IcProducts.ItemsSource = AllProducts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // CART LOGIC
        // ==========================================
        private void Category_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string selectedCategory = clickedButton.Content.ToString();
            if (selectedCategory == "All Categories")
                IcProducts.ItemsSource = AllProducts;
            else
                IcProducts.ItemsSource = new ObservableCollection<CashierProductItem>(AllProducts.Where(p => p.Category == selectedCategory).ToList());
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            CashierProductItem selectedProduct = clickedButton.DataContext as CashierProductItem;

            if (selectedProduct != null)
            {
                // Basic check to see if they have stock before adding to cart
                if (selectedProduct.StockLeft <= 0)
                {
                    MessageBox.Show("This item is currently out of stock!", "Out of Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existingItem = ShoppingCart.FirstOrDefault(c => c.ProductID == selectedProduct.ProductID);
                if (existingItem != null)
                {
                    // Check stock limit for multiples
                    if (existingItem.Quantity + 1 > selectedProduct.StockLeft)
                    {
                        MessageBox.Show("Cannot add more. You have reached the available stock limit for this item.", "Stock Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existingItem.Quantity++;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    ShoppingCart.Add(new CartItem { ProductID = selectedProduct.ProductID, ProductName = selectedProduct.ProductName, UnitPrice = selectedProduct.SellingPrice, Quantity = 1, TotalPrice = selectedProduct.SellingPrice });
                }
                DgdCart.Items.Refresh();
                CalculateTotals();
            }
        }

        private void CalculateTotals()
        {
            decimal subTotal = 0;
            foreach (var item in ShoppingCart) { subTotal += item.TotalPrice; }
            currentGrandTotal = subTotal;
            TxtSubTotal.Text = subTotal.ToString("N2");
            TxtTotal.Text = currentGrandTotal.ToString("N2");
            CalculateChange();
        }

        private void TxtCash_TextChanged(object sender, TextChangedEventArgs e) { CalculateChange(); }

        private void CalculateChange()
        {
            if (decimal.TryParse(TxtCash.Text, out decimal cashGiven))
            {
                decimal change = cashGiven - currentGrandTotal;
                TxtChange.Text = change >= 0 ? change.ToString("N2") : "0.00";
            }
            else { TxtChange.Text = "0.00"; }
        }

        private void BtnClearOrder_Click(object sender, RoutedEventArgs e)
        {
            ShoppingCart.Clear();
            TxtCash.Clear();
            CalculateTotals();
        }

        // ==========================================
        // CHECKOUT & PAYMENT LOGIC
        // ==========================================
        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (ShoppingCart.Count == 0 || !decimal.TryParse(TxtCash.Text, out decimal cashGiven) || cashGiven < currentGrandTotal)
            {
                MessageBox.Show("Invalid Payment. Check cart or cash amount.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ProcessTransaction("Cash", null, cashGiven);
        }

        private void BtnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ShoppingCart.Count == 0) { MessageBox.Show("The cart is empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            DebtorOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelCredit_Click(object sender, RoutedEventArgs e) { DebtorOverlay.Visibility = Visibility.Collapsed; }

        private void BtnConfirmCredit_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtDebtorName.Text.Trim();
            string phone = TxtDebtorPhone.Text.Trim();
            string address = TxtDebtorAddress.Text.Trim();

            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Name and Phone Number are required for a credit sale.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int debtorId = 0;
            string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string checkDebtorQuery = "SELECT DebtorID FROM Debtor WHERE Phone = @Phone";
                    SqlCommand checkCmd = new SqlCommand(checkDebtorQuery, con, transaction);
                    checkCmd.Parameters.AddWithValue("@Phone", phone);
                    object result = checkCmd.ExecuteScalar();

                    if (result != null)
                    {
                        debtorId = Convert.ToInt32(result);
                        string updateDebtorQuery = "UPDATE Debtor SET TotalOutstandingAmount = TotalOutstandingAmount + @Amount WHERE DebtorID = @DebtorID";
                        SqlCommand updateCmd = new SqlCommand(updateDebtorQuery, con, transaction);
                        updateCmd.Parameters.AddWithValue("@Amount", currentGrandTotal);
                        updateCmd.Parameters.AddWithValue("@DebtorID", debtorId);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        string insertDebtorQuery = "INSERT INTO Debtor (Name, Phone, Address, TotalOutstandingAmount, StaffID) VALUES (@Name, @Phone, @Address, @Amount, @StaffID); SELECT SCOPE_IDENTITY();";
                        SqlCommand insertCmd = new SqlCommand(insertDebtorQuery, con, transaction);
                        insertCmd.Parameters.AddWithValue("@Name", name);
                        insertCmd.Parameters.AddWithValue("@Phone", phone);
                        insertCmd.Parameters.AddWithValue("@Address", address);
                        insertCmd.Parameters.AddWithValue("@Amount", currentGrandTotal);
                        insertCmd.Parameters.AddWithValue("@StaffID", loggedInStaffID); // Using dynamic identity
                        debtorId = Convert.ToInt32(insertCmd.ExecuteScalar());
                    }

                    string insertDebtTransQuery = "INSERT INTO Debt_Transaction (Date, Type, Amount, DebtorID, StaffID) VALUES (@Date, 'Credit Added', @Amount, @DebtorID, @StaffID)";
                    SqlCommand transCmd = new SqlCommand(insertDebtTransQuery, con, transaction);
                    transCmd.Parameters.AddWithValue("@Date", DateTime.Now.Date);
                    transCmd.Parameters.AddWithValue("@Amount", currentGrandTotal);
                    transCmd.Parameters.AddWithValue("@DebtorID", debtorId);
                    transCmd.Parameters.AddWithValue("@StaffID", loggedInStaffID);
                    transCmd.ExecuteNonQuery();

                    DebtorOverlay.Visibility = Visibility.Collapsed;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Failed to process Debtor: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            ProcessTransaction("Credit", debtorId, 0);
        }

        private void ProcessTransaction(string paymentType, int? debtorId, decimal cashGiven)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string insertBillQuery = @"
                        INSERT INTO Bill (Date, Time, SubTotal, BillDiscount, GrandTotal, PaymentType, Status, StaffID, DebtorID) 
                        VALUES (@Date, @Time, @SubTotal, @BillDiscount, @GrandTotal, @PaymentType, @Status, @StaffID, @DebtorID);
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand billCmd = new SqlCommand(insertBillQuery, con, transaction);
                    billCmd.Parameters.AddWithValue("@Date", DateTime.Now.Date);
                    billCmd.Parameters.AddWithValue("@Time", DateTime.Now.TimeOfDay);
                    billCmd.Parameters.AddWithValue("@SubTotal", currentGrandTotal);
                    billCmd.Parameters.AddWithValue("@BillDiscount", 0);
                    billCmd.Parameters.AddWithValue("@GrandTotal", currentGrandTotal);
                    billCmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    billCmd.Parameters.AddWithValue("@Status", "Completed");
                    billCmd.Parameters.AddWithValue("@StaffID", loggedInStaffID); // Using dynamic identity

                    if (debtorId.HasValue) billCmd.Parameters.AddWithValue("@DebtorID", debtorId.Value);
                    else billCmd.Parameters.AddWithValue("@DebtorID", DBNull.Value);

                    int generatedBillID = Convert.ToInt32(billCmd.ExecuteScalar());

                    string insertBillItemQuery = "INSERT INTO Bill_Item (QuantitySold, LineDiscount, SellingPriceAtTimeOfSale, BillID, ProductID, BatchID) VALUES (@Qty, @LineDiscount, @SellingPrice, @BillID, @ProductID, @BatchID)";

                    foreach (var item in ShoppingCart)
                    {
                        string getBatchQuery = "SELECT TOP 1 BatchID FROM Batch WHERE ProductID = @ProductID AND CurrentQuantity > 0 ORDER BY ManufacturingDate ASC";
                        SqlCommand batchCmd = new SqlCommand(getBatchQuery, con, transaction);
                        batchCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                        object batchResult = batchCmd.ExecuteScalar();

                        if (batchResult == null) throw new Exception($"No inventory available in Batch table for '{item.ProductName}'!");
                        int batchID = Convert.ToInt32(batchResult);

                        SqlCommand itemCmd = new SqlCommand(insertBillItemQuery, con, transaction);
                        itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@LineDiscount", 0);
                        itemCmd.Parameters.AddWithValue("@SellingPrice", item.UnitPrice);
                        itemCmd.Parameters.AddWithValue("@BillID", generatedBillID);
                        itemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                        itemCmd.Parameters.AddWithValue("@BatchID", batchID);
                        itemCmd.ExecuteNonQuery();

                        string updateBatchQuery = "UPDATE Batch SET CurrentQuantity = CurrentQuantity - @Qty WHERE BatchID = @BatchID";
                        SqlCommand updateCmd = new SqlCommand(updateBatchQuery, con, transaction);
                        updateCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                        updateCmd.Parameters.AddWithValue("@BatchID", batchID);
                        updateCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    TxtReceiptBillID.Text = "#" + generatedBillID.ToString();
                    TxtReceiptDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    IcReceiptItems.ItemsSource = ShoppingCart;
                    TxtReceiptTotal.Text = currentGrandTotal.ToString("N2");

                    if (paymentType == "Credit")
                    {
                        MessageBox.Show("Debtor added/updated successfully!\nThe bill will now be printed as Unpaid.", "Debtor Logged", MessageBoxButton.OK, MessageBoxImage.Information);
                        TxtReceiptCash.Text = "CREDIT SALE (NOT PAID)";
                        TxtReceiptChange.Text = "Charged to Account";
                    }
                    else
                    {
                        TxtReceiptCash.Text = "CASH: " + cashGiven.ToString("N2");
                        TxtReceiptChange.Text = TxtChange.Text;
                    }

                    // Force a refresh of the products so the Stock Left number updates instantly!
                    AllProducts.Clear();
                    LoadProductsFromDatabase();

                    ReceiptOverlay.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCloseReceipt_Click(object sender, RoutedEventArgs e)
        {
            ReceiptOverlay.Visibility = Visibility.Collapsed;
            BtnClearOrder_Click(null, null);
            TxtDebtorName.Clear(); TxtDebtorPhone.Clear(); TxtDebtorAddress.Clear();
        }

        private void BtnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    ReceiptActionGrid.Visibility = Visibility.Collapsed;
                    printDialog.PrintVisual(ReceiptPaper, "Amarasinghe Hardware Receipt");
                    ReceiptActionGrid.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex) { ReceiptActionGrid.Visibility = Visibility.Visible; MessageBox.Show("Print failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e) { MainWindow loginWindow = new MainWindow(); loginWindow.Show(); this.Close(); }
    }

    // ==========================================
    // DATA MODELS
    // ==========================================
    public class CashierProductItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SellingPrice { get; set; }
        public string Category { get; set; }
        public int StockLeft { get; set; }
        public string ImagePath { get; set; }
    }

    // We KEEP CartItem here because it belongs to the Cashier namespace!
    public class CartItem { public int ProductID { get; set; } public string ProductName { get; set; } public decimal UnitPrice { get; set; } public int Quantity { get; set; } public decimal TotalPrice { get; set; } }
}