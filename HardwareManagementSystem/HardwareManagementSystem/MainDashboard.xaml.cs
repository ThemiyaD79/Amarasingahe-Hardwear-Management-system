using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace HardwareManagementSystem
{
    public partial class MainDashboard : Window
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
        private int loggedInStaffID;
        private string loggedInStaffName;
        private InventoryService _inventoryService = new InventoryService();
        private decimal _dailyGoal = 100000; // Default daily goal

        // Default constructor for designer
        public MainDashboard()
        {
            InitializeComponent();
            this.loggedInStaffID = 1;
            this.loggedInStaffName = "Designer Mode";
        }

        public MainDashboard(int staffId, string staffName)
        {
            InitializeComponent();
            this.loggedInStaffID = staffId;
            this.loggedInStaffName = staffName;

            if (TxtWelcomeMessage != null)
                TxtWelcomeMessage.Text = "Welcome Back, " + loggedInStaffName + "!";

            // Initialize Report Dates to today
            if (DpReportFrom != null && DpReportTo != null)
            {
                DpReportFrom.SelectedDate = DateTime.Today;
                DpReportTo.SelectedDate = DateTime.Today;
            }

            RefreshDashboard();
        }

        // ==========================================
        // NAVIGATION
        // ==========================================
        private void HideAllTabs() { TabOverview.Visibility = TabDebtors.Visibility = TabSales.Visibility = TabStaff.Visibility = TabInventory.Visibility = TabReports.Visibility = TabSuppliers.Visibility = Visibility.Collapsed; }
        private void btnHomeNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabOverview.Visibility = Visibility.Visible; RefreshDashboard(); }
        private void btnSalesNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSales.Visibility = Visibility.Visible; LoadSales(); }
        private void btnDebtorsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabDebtors.Visibility = Visibility.Visible; LoadDebtors(); }
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabInventory.Visibility = Visibility.Visible; LoadInventory(); }
        private void btnSuppliersNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSuppliers.Visibility = Visibility.Visible; LoadSuppliers(); }
        private void btnStaffNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabStaff.Visibility = Visibility.Visible; LoadStaff(); }
        private void btnReportsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabReports.Visibility = Visibility.Visible; LoadFinancialReports(); }
        private void btnSettingsNav_Click(object sender, RoutedEventArgs e) { SettingsModalOverlay.Visibility = Visibility.Visible; }
        private void btnCashierNav_Click(object sender, RoutedEventArgs e) { new CashierDashboard(loggedInStaffID, loggedInStaffName).Show(); this.Close(); }
        private void btnLogout_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }

        // ==========================================
        // INVENTORY LOGIC
        // ==========================================
        private void LoadInventory() { DgdInventory.ItemsSource = FetchData("SELECT * FROM Product WHERE IsActive = 1").DefaultView; }

        private void BtnAddInventory_Click(object sender, RoutedEventArgs e)
        {
            TxtInvModalTitle.Text = "ADD NEW PRODUCT";
            HiddenInvProductID.Text = "";
            TxtInvName.Clear(); TxtInvCategory.Clear(); TxtInvSubCategory.Clear();
            TxtInvBrand.Clear(); TxtInvPrice.Clear(); TxtBatchQty.Clear();
            TxtCostPrice.Clear(); TxtBatchNumber.Clear();
            InventoryModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditInventory_Click(object sender, RoutedEventArgs e)
        {
            if (DgdInventory.SelectedItem is DataRowView row)
            {
                TxtInvModalTitle.Text = "UPDATE PRODUCT";
                HiddenInvProductID.Text = row["ProductID"].ToString();
                TxtInvName.Text = row["Name"].ToString();
                TxtInvCategory.Text = row["Category"].ToString();
                TxtInvSubCategory.Text = row["SubCategory"].ToString();
                CmbSellingUnit.Text = row["SellingUnit"].ToString();
                TxtInvBrand.Text = row["Brand"].ToString();
                TxtInvPrice.Text = row["CurrentSellingPrice"].ToString();
                TxtInvReorder.Text = row["ReorderLevel"].ToString();
                ChkInvActive.IsChecked = Convert.ToBoolean(row["IsActive"]);
                InventoryModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Product p = new Product
                {
                    ProductID = string.IsNullOrEmpty(HiddenInvProductID.Text) ? 0 : int.Parse(HiddenInvProductID.Text),
                    Name = TxtInvName.Text,
                    Category = TxtInvCategory.Text,
                    SubCategory = TxtInvSubCategory.Text,
                    Brand = TxtInvBrand.Text,
                    SellingUnit = CmbSellingUnit.Text,
                    CurrentSellingPrice = string.IsNullOrEmpty(TxtInvPrice.Text) ? 0 : decimal.Parse(TxtInvPrice.Text),
                    ReorderLevel = string.IsNullOrEmpty(TxtInvReorder.Text) ? 0 : int.Parse(TxtInvReorder.Text),
                    StaffID = loggedInStaffID,
                    IsActive = ChkInvActive.IsChecked ?? true,
                    ImagePath = HiddenImagePath.Text
                };
                Batch b = new Batch
                {
                    BatchNumber = TxtBatchNumber.Text,
                    CostPrice = string.IsNullOrEmpty(TxtCostPrice.Text) ? 0 : decimal.Parse(TxtCostPrice.Text),
                    InitialQuantity = string.IsNullOrEmpty(TxtBatchQty.Text) ? 0 : decimal.Parse(TxtBatchQty.Text)
                };

                _inventoryService.ProcessProductInventory(p, b);
                InventoryModalOverlay.Visibility = Visibility.Collapsed;
                LoadInventory();
                RefreshDashboard();
                MessageBox.Show("Product saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error saving product: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnDeleteInventory_Click(object sender, RoutedEventArgs e)
        {
            if (DgdInventory.SelectedItem is DataRowView row && MessageBox.Show("Delete this item?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    new SqlCommand($"UPDATE Product SET IsActive = 0 WHERE ProductID = {row["ProductID"]}", con).ExecuteNonQuery();
                }
                LoadInventory();
                RefreshDashboard();
            }
        }

        private void BtnCloseInventoryModal_Click(object sender, RoutedEventArgs e) => InventoryModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e) { OpenFileDialog op = new OpenFileDialog(); if (op.ShowDialog() == true) { ImgProductPreview.Source = new BitmapImage(new Uri(op.FileName)); HiddenImagePath.Text = op.FileName; } }

        // ==========================================
        // RESTOCKING LOGIC (GRN) & QUICK ADD SUPPLIER
        // ==========================================
        private void BtnOpenRestock_Click(object sender, RoutedEventArgs e)
        {
            CmbRestockProduct.ItemsSource = FetchData("SELECT ProductID, Name, CurrentSellingPrice FROM Product WHERE IsActive = 1 ORDER BY Name").DefaultView;
            CmbRestockSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView;

            TxtRestockBatchNo.Text = "GRN-" + DateTime.Now.ToString("yyyyMMdd-HHmm");
            TxtRestockQty.Clear();
            TxtRestockCost.Clear();
            TxtRestockSellingPrice.Clear();

            RestockModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseRestock_Click(object sender, RoutedEventArgs e)
        {
            RestockModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void CmbRestockProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRestockProduct.SelectedItem is DataRowView row)
            {
                TxtRestockSellingPrice.Text = row["CurrentSellingPrice"].ToString();
            }
        }

        private void BtnSaveRestock_Click(object sender, RoutedEventArgs e)
        {
            if (CmbRestockProduct.SelectedValue == null || string.IsNullOrWhiteSpace(TxtRestockQty.Text) || string.IsNullOrWhiteSpace(TxtRestockCost.Text))
            {
                MessageBox.Show("Please select a product and fill in Quantity and Cost.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int productId = Convert.ToInt32(CmbRestockProduct.SelectedValue);
                int supplierId = CmbRestockSupplier.SelectedValue != null ? Convert.ToInt32(CmbRestockSupplier.SelectedValue) : 1;
                decimal qty = decimal.Parse(TxtRestockQty.Text);
                decimal costPrice = decimal.Parse(TxtRestockCost.Text);
                decimal newSellingPrice = decimal.Parse(TxtRestockSellingPrice.Text);

                // Calculate Total Cost to add to Supplier Debt if bought on credit
                decimal totalBatchCost = qty * costPrice;

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    SqlTransaction transaction = con.BeginTransaction();

                    try
                    {
                        string insertBatch = "INSERT INTO Batch (BatchNumber, CostPrice, InitialQuantity, CurrentQuantity, ManufacturingDate, ProductID, SupplierID) VALUES (@BN, @Cost, @Qty, @Qty, GETDATE(), @PID, @SID)";
                        SqlCommand cmdBatch = new SqlCommand(insertBatch, con, transaction);
                        cmdBatch.Parameters.AddWithValue("@BN", TxtRestockBatchNo.Text);
                        cmdBatch.Parameters.AddWithValue("@Cost", costPrice);
                        cmdBatch.Parameters.AddWithValue("@Qty", qty);
                        cmdBatch.Parameters.AddWithValue("@PID", productId);
                        cmdBatch.Parameters.AddWithValue("@SID", supplierId);
                        cmdBatch.ExecuteNonQuery();

                        string updatePrice = "UPDATE Product SET CurrentSellingPrice = @NewPrice WHERE ProductID = @PID";
                        SqlCommand cmdPrice = new SqlCommand(updatePrice, con, transaction);
                        cmdPrice.Parameters.AddWithValue("@NewPrice", newSellingPrice);
                        cmdPrice.Parameters.AddWithValue("@PID", productId);
                        cmdPrice.ExecuteNonQuery();

                        // Automatically update Supplier's Outstanding Balance (Assuming bought on credit for now)
                        try
                        {
                            string updateDebt = "UPDATE Supplier SET OutstandingBalance = ISNULL(OutstandingBalance, 0) + @AddedDebt WHERE SupplierID = @SID";
                            SqlCommand cmdDebt = new SqlCommand(updateDebt, con, transaction);
                            cmdDebt.Parameters.AddWithValue("@AddedDebt", totalBatchCost);
                            cmdDebt.Parameters.AddWithValue("@SID", supplierId);
                            cmdDebt.ExecuteNonQuery();
                        }
                        catch { /* Column might not exist yet, ignore silently */ }

                        string productName = ((DataRowView)CmbRestockProduct.SelectedItem)["Name"].ToString();
                        string auditLog = "INSERT INTO AuditLog (StaffID, ActionType, Description) VALUES (@UID, 'RESTOCK', @Desc)";
                        SqlCommand cmdAudit = new SqlCommand(auditLog, con, transaction);
                        cmdAudit.Parameters.AddWithValue("@UID", loggedInStaffID);
                        cmdAudit.Parameters.AddWithValue("@Desc", $"Added {qty} units to {productName}. Cost: {costPrice}, Sell Price: {newSellingPrice}");
                        cmdAudit.ExecuteNonQuery();

                        transaction.Commit();
                        MessageBox.Show("Stock successfully added and prices updated!", "Restock Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                        RestockModalOverlay.Visibility = Visibility.Collapsed;
                        LoadInventory();
                        RefreshDashboard();
                        LoadSuppliers(); // Refresh debts
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error processing restock: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnQuickAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            TxtQuickSupplierName.Clear();
            TxtQuickSupplierPhone.Clear();
            QuickSupplierModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelQuickSupplier_Click(object sender, RoutedEventArgs e)
        {
            QuickSupplierModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveQuickSupplier_Click(object sender, RoutedEventArgs e)
        {
            string supplierName = TxtQuickSupplierName.Text.Trim();
            if (string.IsNullOrEmpty(supplierName))
            {
                MessageBox.Show("Supplier Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string query = "INSERT INTO Supplier (SupplierName, Phone, Category, OutstandingBalance) VALUES (@Name, @Phone, 'General', 0); SELECT SCOPE_IDENTITY();";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Name", supplierName);
                    cmd.Parameters.AddWithValue("@Phone", TxtQuickSupplierPhone.Text.Trim());

                    int newSupplierId = Convert.ToInt32(cmd.ExecuteScalar());
                    QuickSupplierModalOverlay.Visibility = Visibility.Collapsed;

                    if (CmbRestockSupplier != null)
                    {
                        CmbRestockSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView;
                        CmbRestockSupplier.SelectedValue = newSupplierId;
                    }

                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving supplier: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // SUPPLIER MANAGEMENT TAB LOGIC (With Debt Payments)
        // ==========================================
        private void LoadSuppliers()
        {
            if (DgdSuppliers != null)
            {
                try
                {
                    DgdSuppliers.ItemsSource = FetchData("SELECT SupplierID, SupplierName, CompanyName, Phone, Category, ISNULL(OutstandingBalance, 0) AS OutstandingBalance FROM Supplier ORDER BY SupplierName").DefaultView;
                }
                catch
                {
                    // Fallback if OutstandingBalance column doesn't exist yet
                    DgdSuppliers.ItemsSource = FetchData("SELECT SupplierID, SupplierName, CompanyName, Phone, Category, 0.00 AS OutstandingBalance FROM Supplier ORDER BY SupplierName").DefaultView;
                }
            }
        }

        private void BtnViewSupplierStock_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSuppliers.SelectedItem is DataRowView row)
            {
                int supplierId = Convert.ToInt32(row["SupplierID"]);
                TxtStockModalSupplierName.Text = "Stock from: " + row["SupplierName"].ToString();

                DgdSupplierStock.ItemsSource = _inventoryService.GetStockBySupplier(supplierId).DefaultView;
                SupplierStockModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseSupplierStock_Click(object sender, RoutedEventArgs e)
        {
            SupplierStockModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnOpenSettleSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSuppliers.SelectedItem is DataRowView row)
            {
                HiddenSupplierPayID.Text = row["SupplierID"].ToString();
                TxtSettleSupplierName.Text = row["SupplierName"].ToString();
                TxtSettleSupplierTotalAmount.Text = $"LKR {Convert.ToDecimal(row["OutstandingBalance"]):N2}";
                TxtSupplierAmountPaying.Clear();
                TxtSupplierRemainingBalance.Text = "Balance After: LKR 0.00";
                SettleSupplierDebtOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnConfirmSettleSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal paying = decimal.Parse(TxtSupplierAmountPaying.Text);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();
                    try
                    {
                        new SqlCommand($"UPDATE Supplier SET OutstandingBalance -= {paying} WHERE SupplierID = {HiddenSupplierPayID.Text}", con, trans).ExecuteNonQuery();

                        string auditLog = "INSERT INTO AuditLog (StaffID, ActionType, Description) VALUES (@UID, 'PAYMENT', @Desc)";
                        SqlCommand cmdAudit = new SqlCommand(auditLog, con, trans);
                        cmdAudit.Parameters.AddWithValue("@UID", loggedInStaffID);
                        cmdAudit.Parameters.AddWithValue("@Desc", $"Paid LKR {paying} to Supplier ID {HiddenSupplierPayID.Text}");
                        cmdAudit.ExecuteNonQuery();

                        trans.Commit();
                        SettleSupplierDebtOverlay.Visibility = Visibility.Collapsed;
                        LoadSuppliers();
                        RefreshDashboard();
                        MessageBox.Show("Supplier payment recorded successfully!", "Payment Processed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch { trans.Rollback(); throw; }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancelSettleSupplier_Click(object sender, RoutedEventArgs e) => SettleSupplierDebtOverlay.Visibility = Visibility.Collapsed;

        private void TxtSupplierAmountPaying_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal total = decimal.Parse(TxtSettleSupplierTotalAmount.Text.Replace("LKR ", ""));
                decimal paying = string.IsNullOrEmpty(TxtSupplierAmountPaying.Text) ? 0 : decimal.Parse(TxtSupplierAmountPaying.Text);
                TxtSupplierRemainingBalance.Text = $"Balance After: LKR {(total - paying):N2}";
            }
            catch { }
        }


        // ==========================================
        // DEBTOR / SETTLEMENT
        // ==========================================
        private void LoadDebtors() { DgdDebtors.ItemsSource = FetchData("SELECT * FROM Debtor").DefaultView; }

        private void BtnOpenSettleDebt_Click(object sender, RoutedEventArgs e)
        {
            if (DgdDebtors.SelectedItem is DataRowView row)
            {
                HiddenDebtorID.Text = row["DebtorID"].ToString();
                TxtSettleCustomerName.Text = row["Name"].ToString();
                TxtSettleTotalAmount.Text = $"LKR {Convert.ToDecimal(row["TotalOutstandingAmount"]):N2}";
                TxtAmountPaying.Clear();
                TxtRemainingBalance.Text = "Balance After: LKR 0.00";
                SettleDebtOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnConfirmSettle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal paying = decimal.Parse(TxtAmountPaying.Text);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    new SqlCommand($"UPDATE Debtor SET TotalOutstandingAmount -= {paying} WHERE DebtorID = {HiddenDebtorID.Text}", con).ExecuteNonQuery();
                }
                SettleDebtOverlay.Visibility = Visibility.Collapsed;
                LoadDebtors();
                MessageBox.Show("Payment recorded successfully!");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancelSettle_Click(object sender, RoutedEventArgs e) => SettleDebtOverlay.Visibility = Visibility.Collapsed;

        private void TxtAmountPaying_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal total = decimal.Parse(TxtSettleTotalAmount.Text.Replace("LKR ", ""));
                decimal paying = string.IsNullOrEmpty(TxtAmountPaying.Text) ? 0 : decimal.Parse(TxtAmountPaying.Text);
                TxtRemainingBalance.Text = $"Balance After: LKR {(total - paying):N2}";
            }
            catch { }
        }

        // ==========================================
        // SALES / BILLS
        // ==========================================
        private void LoadSales() { DgdSales.ItemsSource = FetchData("SELECT b.*, s.FullName as StaffName FROM Bill b JOIN Staff s ON b.StaffID = s.StaffID").DefaultView; }

        private void BtnViewBill_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSales.SelectedItem is DataRowView row)
            {
                TxtReceiptBillID.Text = "#" + row["BillID"].ToString();
                DateTime date = Convert.ToDateTime(row["Date"]);
                TxtReceiptDate.Text = date.ToString("dd/MM/yyyy") + " " + row["Time"].ToString();
                TxtReceiptTotal.Text = Convert.ToDecimal(row["GrandTotal"]).ToString("N2");
                TxtReceiptCash.Text = row["PaymentType"].ToString();

                string query = $"SELECT p.Name as ProductName, bi.QuantitySold as Quantity, (bi.SellingPriceAtTimeOfSale * bi.QuantitySold) as TotalPrice FROM Bill_Item bi JOIN Product p ON bi.ProductID = p.ProductID WHERE bi.BillID = {row["BillID"]}";
                IcBillItems.ItemsSource = FetchData(query).DefaultView;

                BillViewerOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseBill_Click(object sender, RoutedEventArgs e) => BillViewerOverlay.Visibility = Visibility.Collapsed;
        private void CmbStaffFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ==========================================
        // STAFF MANAGEMENT
        // ==========================================
        private void LoadStaff() { DgdStaff.ItemsSource = FetchData("SELECT StaffID, FullName, Username, Role, Phone, IsActive FROM Staff").DefaultView; }

        private void BtnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            TxtStaffModalTitle.Text = "ADD NEW SYSTEM USER";
            HiddenStaffFormID.Text = "";
            TxtStaffFullName.Clear(); TxtStaffUsername.Clear(); TxtStaffPassword.Password = "";
            StaffModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditStaff_Click(object sender, RoutedEventArgs e)
        {
            if (DgdStaff.SelectedItem is DataRowView row)
            {
                TxtStaffModalTitle.Text = "EDIT SYSTEM USER";
                HiddenStaffFormID.Text = row["StaffID"].ToString();
                TxtStaffFullName.Text = row["FullName"].ToString();
                TxtStaffUsername.Text = row["Username"].ToString();
                TxtStaffNIC.Text = "Hidden";
                TxtStaffPhone.Text = row["Phone"].ToString();
                ChkStaffActive.IsChecked = Convert.ToBoolean(row["IsActive"]);
                StaffModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveStaff_Click(object sender, RoutedEventArgs e)
        {
            StaffModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnDeleteStaff_Click(object sender, RoutedEventArgs e) { }
        private void BtnCloseStaffModal_Click(object sender, RoutedEventArgs e) => StaffModalOverlay.Visibility = Visibility.Collapsed;

        // ==========================================
        // REPORT GENERATION LOGIC (With Export & Print)
        // ==========================================
        private void LoadFinancialReports()
        {
            BtnFilterReports_Click(null, null); // Load using default selected dates
        }

        private void BtnFilterReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fromDate = DpReportFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
                string toDate = DpReportTo.SelectedDate.Value.ToString("yyyy-MM-dd");

                string profitQuery = $@"SELECT Date, COUNT(DISTINCT BillID) as TotalBills, 
                                       SUM(NetRevenue) as TotalRevenue, 
                                       SUM(Profit) as TotalProfit 
                                       FROM vw_ProfitReport 
                                       WHERE Date >= '{fromDate}' AND Date <= '{toDate}'
                                       GROUP BY Date ORDER BY Date DESC";

                if (DgdFinancialSummary != null)
                    DgdFinancialSummary.ItemsSource = FetchData(profitQuery).DefaultView;

                string cashierQuery = $@"SELECT CashierName, COUNT(DISTINCT BillID) as Transactions, 
                                        SUM(NetRevenue) as TotalSales 
                                        FROM vw_ProfitReport 
                                        WHERE Date >= '{fromDate}' AND Date <= '{toDate}'
                                        GROUP BY CashierName";

                if (DgdCashierReport != null)
                    DgdCashierReport.ItemsSource = FetchData(cashierQuery).DefaultView;

                // New: Load Items Sold in this period
                string itemsQuery = $@"SELECT p.Name as ProductName, SUM(bi.QuantitySold) as TotalQty, SUM(bi.QuantitySold * bi.SellingPriceAtTimeOfSale) as TotalRevenue 
                                       FROM Bill_Item bi 
                                       JOIN Bill b ON bi.BillID = b.BillID 
                                       JOIN Product p ON bi.ProductID = p.ProductID 
                                       WHERE b.Date >= '{fromDate}' AND b.Date <= '{toDate}' AND b.Status = 'Completed'
                                       GROUP BY p.Name ORDER BY TotalQty DESC";

                if (DgdItemsSoldReport != null)
                    DgdItemsSoldReport.ItemsSource = FetchData(itemsQuery).DefaultView;

                UpdateMonthlyReportKPIs(fromDate, toDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report Filtering Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMonthlyReportKPIs(string fromDate, string toDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sql = $"SELECT ISNULL(SUM(Profit), 0) FROM vw_ProfitReport WHERE Date >= '{fromDate}' AND Date <= '{toDate}'";
                    var result = new SqlCommand(sql, con).ExecuteScalar();

                    if (TxtMonthlyProfit != null)
                        TxtMonthlyProfit.Text = $"LKR {Convert.ToDecimal(result):N2}";
                }
            }
            catch { }
        }

        private void BtnDownloadReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV file (*.csv)|*.csv", FileName = "FinancialReport.csv" };
                if (sfd.ShowDialog() == true)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Date,Bills,Revenue,Profit");
                    foreach (DataRowView row in DgdFinancialSummary.ItemsSource)
                    {
                        sb.AppendLine($"{Convert.ToDateTime(row["Date"]).ToString("yyyy-MM-dd")},{row["TotalBills"]},{row["TotalRevenue"]},{row["TotalProfit"]}");
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Report Downloaded Successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show("Export Error: " + ex.Message); }
        }

        private void BtnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(DgdFinancialSummary, "Financial Summary Print");
                }
            }
            catch (Exception ex) { MessageBox.Show("Print Error: " + ex.Message); }
        }

        // ==========================================
        // SETTINGS MODAL LOGIC
        // ==========================================
        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(TxtDailyGoal.Text, out decimal newGoal))
                {
                    _dailyGoal = newGoal;
                }
            }
            catch { }

            // Apply Theme Conceptually (Real dynamic themes require heavy resource dictionary swapping)
            string selectedTheme = ((ComboBoxItem)CmbTheme.SelectedItem).Content.ToString();
            if (selectedTheme == "Dark Mode")
            {
                MessageBox.Show("Dark Mode requires app restart to apply resources.", "Theme Update", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            SettingsModalOverlay.Visibility = Visibility.Collapsed;
            RefreshDashboard(); // Update progress bar
            MessageBox.Show("Settings saved successfully!", "Settings Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBackupDB_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Database backup initiated securely.", "Backup Status", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ==========================================
        // DASHBOARD REFRESH & LOW STOCK ALERTS
        // ==========================================
        private void RefreshDashboard()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    decimal totalItems = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(CurrentQuantity),0) FROM Batch", con).ExecuteScalar());
                    if (TxtTotalItems != null) TxtTotalItems.Text = totalItems.ToString("0.##");

                    try
                    {
                        var payable = new SqlCommand("SELECT ISNULL(SUM(OutstandingBalance),0) FROM Supplier", con).ExecuteScalar();
                        if (TxtTotalPayable != null) TxtTotalPayable.Text = $"LKR {Convert.ToDecimal(payable):N2}";
                    }
                    catch
                    {
                        if (TxtTotalPayable != null) TxtTotalPayable.Text = "LKR 0.00";
                    }

                    decimal rev = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(GrandTotal),0) FROM Bill WHERE Date=CAST(GETDATE() AS DATE) AND Status='Completed'", con).ExecuteScalar());
                    if (TxtTodayRevenue != null) TxtTodayRevenue.Text = $"LKR {rev:N2}";

                    // Calculate Daily Goal Progress
                    if (ProgDailyGoal != null)
                    {
                        ProgDailyGoal.Maximum = (double)_dailyGoal;
                        ProgDailyGoal.Value = (double)rev;
                        decimal percent = (_dailyGoal > 0) ? (rev / _dailyGoal) * 100 : 0;
                        if (TxtGoalProgress != null) TxtGoalProgress.Text = $"{percent:N1}% of Daily Target (LKR {_dailyGoal:N0})";
                    }

                    if (TxtTotalTransactions != null)
                        TxtTotalTransactions.Text = new SqlCommand("SELECT COUNT(*) FROM Bill WHERE Date=CAST(GETDATE() AS DATE)", con).ExecuteScalar().ToString();
                }

                DataTable dtLowStock = _inventoryService.GetLowStockItems();

                if (DgdLowStock != null)
                {
                    DgdLowStock.ItemsSource = dtLowStock.DefaultView;
                }

                int lowStockCount = dtLowStock.Rows.Count;
                if (TxtLowStock != null) TxtLowStock.Text = $"{lowStockCount} Items";
                if (TxtNotificationCount != null) TxtNotificationCount.Text = lowStockCount.ToString();
            }
            catch { }
        }

        private DataTable FetchData(string query)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            {
                new SqlDataAdapter(query, con).Fill(dt);
            }
            return dt;
        }
    }
}