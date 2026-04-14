using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media;

namespace HardwareManagementSystem
{
    public partial class CashierDashboard : Window
    {
        public ObservableCollection<CartItem> ShoppingCart { get; set; } = new ObservableCollection<CartItem>();
        private ObservableCollection<Product> AllProducts { get; set; } = new ObservableCollection<Product>();

        private List<string> activeCategories = new List<string>();
        private List<string> activeSubCategories = new List<string>();

        private decimal currentSubTotal = 0;
        private decimal currentLineDiscounts = 0;
        private decimal currentBillDiscount = 0;
        private decimal currentGrandTotal = 0;

        private int loggedInStaffID;
        private string loggedInStaffName;

        private Product pendingProductToAdd;
        private CartItem itemBeingEdited;

        public CashierDashboard(int staffId = 4, string staffName = "Cashier")
        {
            InitializeComponent();
            loggedInStaffID = staffId;
            loggedInStaffName = staffName;

            if (TxtCurrentUserName != null)
                TxtCurrentUserName.Text = loggedInStaffName;

            DgdCart.ItemsSource = ShoppingCart;
            LoadProductsFromDatabase();
        }

        private void LoadProductsFromDatabase()
        {
            try
            {
                AllProducts.Clear();
                string connectionString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT p.ProductID, p.Name, p.CurrentSellingPrice, p.Category, p.SubCategory, p.SellingUnit, p.ImagePath, 
                               ISNULL(SUM(b.CurrentQuantity), 0) AS TotalQuantity 
                        FROM Product p 
                        LEFT JOIN Batch b ON p.ProductID = b.ProductID 
                        WHERE p.IsActive = 1 
                        GROUP BY p.ProductID, p.Name, p.CurrentSellingPrice, p.Category, p.SubCategory, p.SellingUnit, p.ImagePath";

                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        AllProducts.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            Name = reader["Name"].ToString(),
                            CurrentSellingPrice = Convert.ToDecimal(reader["CurrentSellingPrice"]),
                            Category = reader["Category"].ToString(),
                            SubCategory = reader["SubCategory"] != DBNull.Value ? reader["SubCategory"].ToString() : "",
                            SellingUnit = reader["SellingUnit"].ToString(),
                            TotalQuantity = Convert.ToDecimal(reader["TotalQuantity"]),
                            ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : null
                        });
                    }
                    IcProducts.ItemsSource = AllProducts;
                    PopulateFilterCheckboxes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFilterCheckboxes()
        {
            if (SpCategories == null || SpSubCategories == null) return;

            SpCategories.Children.Clear();
            SpSubCategories.Children.Clear();

            var categories = AllProducts.Where(p => !string.IsNullOrWhiteSpace(p.Category)).Select(p => p.Category).Distinct().ToList();
            var subCategories = AllProducts.Where(p => !string.IsNullOrWhiteSpace(p.SubCategory)).Select(p => p.SubCategory).Distinct().ToList();

            foreach (var cat in categories)
            {
                CheckBox cb = new CheckBox { Content = cat, Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)), Margin = new Thickness(10, 4, 0, 4), FontSize = 14, Cursor = System.Windows.Input.Cursors.Hand };
                cb.Checked += (s, e) => { activeCategories.Add(cat); ApplyFilters(); };
                cb.Unchecked += (s, e) => { activeCategories.Remove(cat); ApplyFilters(); };
                SpCategories.Children.Add(cb);
            }

            foreach (var sub in subCategories)
            {
                CheckBox cb = new CheckBox { Content = sub, Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)), Margin = new Thickness(10, 4, 0, 4), FontSize = 14, Cursor = System.Windows.Input.Cursors.Hand };
                cb.Checked += (s, e) => { activeSubCategories.Add(sub); ApplyFilters(); };
                cb.Unchecked += (s, e) => { activeSubCategories.Remove(sub); ApplyFilters(); };
                SpSubCategories.Children.Add(cb);
            }
        }

        private void ApplyFilters()
        {
            if (AllProducts == null || IcProducts == null) return;

            string searchText = TxtProductSearch.Text.ToLower();
            if (searchText == "search products...") searchText = "";

            var filtered = AllProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
                filtered = filtered.Where(p => p.Name.ToLower().Contains(searchText) || p.Brand?.ToLower().Contains(searchText) == true);

            if (activeCategories.Count > 0)
                filtered = filtered.Where(p => activeCategories.Contains(p.Category));

            if (activeSubCategories.Count > 0)
                filtered = filtered.Where(p => activeSubCategories.Contains(p.SubCategory));

            IcProducts.ItemsSource = new ObservableCollection<Product>(filtered);
        }

        private void TxtProductSearch_TextChanged(object sender, TextChangedEventArgs e) { ApplyFilters(); }
        private void TxtProductSearch_GotFocus(object sender, RoutedEventArgs e) { if (TxtProductSearch.Text == "Search products...") TxtProductSearch.Text = ""; }
        private void TxtProductSearch_LostFocus(object sender, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(TxtProductSearch.Text)) TxtProductSearch.Text = "Search products..."; }
        private void Category_Click(object sender, RoutedEventArgs e) { } // Legacy button support

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            activeCategories.Clear();
            activeSubCategories.Clear();
            TxtProductSearch.Text = "Search products...";
            foreach (var child in SpCategories.Children) { if (child is CheckBox cb) cb.IsChecked = false; }
            foreach (var child in SpSubCategories.Children) { if (child is CheckBox cb) cb.IsChecked = false; }
            ApplyFilters();
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            pendingProductToAdd = clickedButton.DataContext as Product;

            if (pendingProductToAdd != null)
            {
                if (pendingProductToAdd.TotalQuantity <= 0)
                {
                    MessageBox.Show("This item is currently out of stock!", "Out of Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TxtAddProductName.Text = pendingProductToAdd.Name;
                TxtAddProductStock.Text = $"Stock Available: {pendingProductToAdd.TotalQuantity:0.##} {pendingProductToAdd.SellingUnit}s";
                TxtAddUnit.Text = pendingProductToAdd.SellingUnit;
                TxtAddQuantity.Text = "1";
                TxtAddQuantity.Focus();
                TxtAddQuantity.SelectAll();

                AddToCartOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelAdd_Click(object sender, RoutedEventArgs e)
        {
            AddToCartOverlay.Visibility = Visibility.Collapsed;
            pendingProductToAdd = null;
        }

        private void BtnConfirmAdd_Click(object sender, RoutedEventArgs e)
        {
            if (pendingProductToAdd == null) return;

            if (!decimal.TryParse(TxtAddQuantity.Text, out decimal desiredQty) || desiredQty <= 0)
            {
                MessageBox.Show("Please enter a valid number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingItem = ShoppingCart.FirstOrDefault(c => c.ProductID == pendingProductToAdd.ProductID);
            decimal currentQtyInCart = existingItem != null ? existingItem.Quantity : 0;

            if (currentQtyInCart + desiredQty > pendingProductToAdd.TotalQuantity)
            {
                MessageBox.Show($"Cannot add {desiredQty}. You only have {pendingProductToAdd.TotalQuantity:0.##} in stock.", "Stock Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity += desiredQty;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                ShoppingCart.Add(new CartItem
                {
                    ProductID = pendingProductToAdd.ProductID,
                    ProductName = pendingProductToAdd.Name,
                    OriginalUnitPrice = pendingProductToAdd.CurrentSellingPrice,
                    UnitPrice = pendingProductToAdd.CurrentSellingPrice,
                    Quantity = desiredQty,
                    SellingUnit = pendingProductToAdd.SellingUnit,
                    TotalPrice = desiredQty * pendingProductToAdd.CurrentSellingPrice
                });
            }

            DgdCart.Items.Refresh();
            CalculateTotals();
            AddToCartOverlay.Visibility = Visibility.Collapsed;
            pendingProductToAdd = null;
        }

        private void BtnRemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            CartItem item = btn.DataContext as CartItem;
            if (item != null) { ShoppingCart.Remove(item); CalculateTotals(); }
        }

        private void BtnEditCartItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            itemBeingEdited = btn.DataContext as CartItem;
            if (itemBeingEdited != null)
            {
                TxtEditProductName.Text = itemBeingEdited.ProductName;
                TxtEditPrice.Text = itemBeingEdited.UnitPrice.ToString("0.00");
                TxtEditQuantity.Text = itemBeingEdited.Quantity.ToString("0.##");
                EditCartItemOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditCartItemOverlay.Visibility = Visibility.Collapsed;
            itemBeingEdited = null;
        }

        private void BtnConfirmEdit_Click(object sender, RoutedEventArgs e)
        {
            if (itemBeingEdited == null) return;

            if (decimal.TryParse(TxtEditPrice.Text, out decimal newPrice) && decimal.TryParse(TxtEditQuantity.Text, out decimal newQty) && newQty > 0)
            {
                Product dbProduct = AllProducts.FirstOrDefault(p => p.ProductID == itemBeingEdited.ProductID);

                if (dbProduct != null && newQty > dbProduct.TotalQuantity && itemBeingEdited.ProductID > 0)
                {
                    MessageBox.Show($"Cannot set quantity to {newQty}. Only {dbProduct.TotalQuantity:0.##} in stock.", "Stock Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                itemBeingEdited.UnitPrice = newPrice;
                itemBeingEdited.Quantity = newQty;
                itemBeingEdited.TotalPrice = newPrice * newQty;

                DgdCart.Items.Refresh();
                CalculateTotals();
                EditCartItemOverlay.Visibility = Visibility.Collapsed;
                itemBeingEdited = null;
            }
            else { MessageBox.Show("Invalid numbers entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void CalculateTotals()
        {
            currentSubTotal = ShoppingCart.Sum(i => i.OriginalUnitPrice * i.Quantity);
            currentLineDiscounts = ShoppingCart.Sum(i => i.LineDiscount);

            if (TxtBillDiscount != null && decimal.TryParse(TxtBillDiscount.Text, out decimal dist)) { currentBillDiscount = dist; }
            else { currentBillDiscount = 0; }

            currentGrandTotal = currentSubTotal - currentLineDiscounts - currentBillDiscount;
            if (currentGrandTotal < 0) currentGrandTotal = 0;

            if (TxtSubTotal != null) TxtSubTotal.Text = currentSubTotal.ToString("N2");
            if (TxtItemDiscounts != null) TxtItemDiscounts.Text = "- " + currentLineDiscounts.ToString("N2");
            if (TxtTotal != null) TxtTotal.Text = currentGrandTotal.ToString("N2");

            CalculateChange();
        }

        private void TxtBillDiscount_TextChanged(object sender, TextChangedEventArgs e) { CalculateTotals(); }
        private void TxtCash_TextChanged(object sender, TextChangedEventArgs e) { CalculateChange(); }

        private void CalculateChange()
        {
            if (TxtCash == null || TxtChange == null) return;
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
            if (TxtCash != null) TxtCash.Clear();
            if (TxtBillDiscount != null) TxtBillDiscount.Text = "0";
            CalculateTotals();
        }

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
                        insertCmd.Parameters.AddWithValue("@StaffID", loggedInStaffID);
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

        // ==========================================
        // UPDATED: TRANSACTION LOGIC TO FIX CUSTOM ITEMS
        // ==========================================
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
                    billCmd.Parameters.AddWithValue("@SubTotal", currentSubTotal);
                    billCmd.Parameters.AddWithValue("@BillDiscount", currentBillDiscount);
                    billCmd.Parameters.AddWithValue("@GrandTotal", currentGrandTotal);
                    billCmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    billCmd.Parameters.AddWithValue("@Status", "Completed");
                    billCmd.Parameters.AddWithValue("@StaffID", loggedInStaffID);

                    if (debtorId.HasValue) billCmd.Parameters.AddWithValue("@DebtorID", debtorId.Value);
                    else billCmd.Parameters.AddWithValue("@DebtorID", DBNull.Value);

                    int generatedBillID = Convert.ToInt32(billCmd.ExecuteScalar());
                    string insertBillItemQuery = "INSERT INTO Bill_Item (QuantitySold, LineDiscount, SellingPriceAtTimeOfSale, BillID, ProductID, BatchID) VALUES (@Qty, @LineDiscount, @SellingPrice, @BillID, @ProductID, @BatchID)";

                    foreach (var item in ShoppingCart)
                    {
                        int currentProductId = item.ProductID;
                        int batchID = 0;

                        // FIX: Check if it's a Manual/Custom Item (Has a negative ID)
                        if (currentProductId < 0)
                        {
                            // Create a hidden "One-Time" product in the DB to satisfy relationships (IsActive = 0)
                            string createDummyProd = "INSERT INTO Product (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID) VALUES (@n, 'Custom Item', 'Manual Entry', @p, 0, 0, 0, @sid); SELECT SCOPE_IDENTITY();";
                            SqlCommand dummyProdCmd = new SqlCommand(createDummyProd, con, transaction);
                            dummyProdCmd.Parameters.AddWithValue("@n", item.ProductName);
                            dummyProdCmd.Parameters.AddWithValue("@p", item.UnitPrice);
                            dummyProdCmd.Parameters.AddWithValue("@sid", loggedInStaffID);
                            currentProductId = Convert.ToInt32(dummyProdCmd.ExecuteScalar());

                            // Create a dummy batch for it to satisfy relationships
                            string createDummyBatch = "INSERT INTO Batch (BatchNumber, CostPrice, InitialQuantity, CurrentQuantity, ManufacturingDate, ProductID, SupplierID) VALUES ('MANUAL', @p, 0, 0, GETDATE(), @pid, 1); SELECT SCOPE_IDENTITY();";
                            SqlCommand dummyBatchCmd = new SqlCommand(createDummyBatch, con, transaction);
                            dummyBatchCmd.Parameters.AddWithValue("@p", item.UnitPrice);
                            dummyBatchCmd.Parameters.AddWithValue("@pid", currentProductId);
                            batchID = Convert.ToInt32(dummyBatchCmd.ExecuteScalar());
                        }
                        else
                        {
                            // Normal Item Logic
                            string getBatchQuery = "SELECT TOP 1 BatchID FROM Batch WHERE ProductID = @ProductID AND CurrentQuantity >= @RequiredQty ORDER BY ManufacturingDate ASC";
                            SqlCommand batchCmd = new SqlCommand(getBatchQuery, con, transaction);
                            batchCmd.Parameters.AddWithValue("@ProductID", currentProductId);
                            batchCmd.Parameters.AddWithValue("@RequiredQty", item.Quantity);
                            object batchResult = batchCmd.ExecuteScalar();

                            if (batchResult == null) throw new Exception($"No inventory available in Batch table for '{item.ProductName}'!");
                            batchID = Convert.ToInt32(batchResult);

                            string updateBatchQuery = "UPDATE Batch SET CurrentQuantity = CurrentQuantity - @Qty WHERE BatchID = @BatchID";
                            SqlCommand updateCmd = new SqlCommand(updateBatchQuery, con, transaction);
                            updateCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                            updateCmd.Parameters.AddWithValue("@BatchID", batchID);
                            updateCmd.ExecuteNonQuery();
                        }

                        // Save Bill Item securely
                        SqlCommand itemCmd = new SqlCommand(insertBillItemQuery, con, transaction);
                        itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@LineDiscount", item.LineDiscount);
                        itemCmd.Parameters.AddWithValue("@SellingPrice", item.UnitPrice);
                        itemCmd.Parameters.AddWithValue("@BillID", generatedBillID);
                        itemCmd.Parameters.AddWithValue("@ProductID", currentProductId);
                        itemCmd.Parameters.AddWithValue("@BatchID", batchID);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    TxtReceiptBillID.Text = "#" + generatedBillID.ToString();
                    TxtReceiptDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    IcReceiptItems.ItemsSource = ShoppingCart;
                    TxtReceiptSubTotal.Text = currentSubTotal.ToString("N2");
                    TxtReceiptItemDiscounts.Text = "- " + currentLineDiscounts.ToString("N2");
                    TxtReceiptDiscount.Text = "- " + currentBillDiscount.ToString("N2");
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
        private void BtnSwitchToManager_Click(object sender, RoutedEventArgs e) { new MainDashboard(loggedInStaffID, loggedInStaffName).Show(); this.Close(); }

        // ==========================================
        // MANUAL / CUSTOM ITEM LOGIC
        // ==========================================
        private void BtnOpenManualItem_Click(object sender, RoutedEventArgs e)
        {
            TxtManualItemName.Clear();
            TxtManualItemPrice.Clear();
            TxtManualItemQuantity.Text = "1";
            CmbManualItemUnit.SelectedIndex = 0;
            ManualItemOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelManualItem_Click(object sender, RoutedEventArgs e)
        {
            ManualItemOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmManualItem_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtManualItemName.Text.Trim();

            if (string.IsNullOrEmpty(name) || !decimal.TryParse(TxtManualItemPrice.Text, out decimal price) || !decimal.TryParse(TxtManualItemQuantity.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid item name, price, and quantity.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Using a negative ID to denote this is a custom, non-database item
            int tempCustomId = (ShoppingCart.Count + 1) * -1;
            string unit = ((ComboBoxItem)CmbManualItemUnit.SelectedItem).Content.ToString();

            ShoppingCart.Add(new CartItem
            {
                ProductID = tempCustomId,
                ProductName = name + " (Custom)",
                OriginalUnitPrice = price,
                UnitPrice = price,
                Quantity = qty,
                SellingUnit = unit,
                TotalPrice = price * qty
            });

            DgdCart.Items.Refresh();
            CalculateTotals();
            ManualItemOverlay.Visibility = Visibility.Collapsed;
        }
    }
}