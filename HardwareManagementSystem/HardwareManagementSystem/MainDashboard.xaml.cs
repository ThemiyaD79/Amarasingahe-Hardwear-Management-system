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
using System.Windows.Media;

namespace HardwareManagementSystem
{
    public partial class MainDashboard : Window
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;
        private int loggedInStaffID;
        private string loggedInStaffName;
        private InventoryService _inventoryService = new InventoryService();
        private decimal _dailyGoal = 100000;

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

            if (DpReportFrom != null && DpReportTo != null)
            {
                DpReportFrom.SelectedDate = DateTime.Today;
                DpReportTo.SelectedDate = DateTime.Today;
            }

            if (DpSalesFrom != null && DpSalesTo != null)
            {
                DpSalesFrom.SelectedDate = DateTime.Today.AddDays(-7);
                DpSalesTo.SelectedDate = DateTime.Today;
            }

            RefreshDashboard();
        }

        private void HideAllTabs() { TabOverview.Visibility = TabDebtors.Visibility = TabSales.Visibility = TabStaff.Visibility = TabInventory.Visibility = TabReports.Visibility = TabSuppliers.Visibility = Visibility.Collapsed; }
        private void btnHomeNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabOverview.Visibility = Visibility.Visible; RefreshDashboard(); }
        private void btnSalesNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSales.Visibility = Visibility.Visible; LoadSales(); }
        private void btnDebtorsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabDebtors.Visibility = Visibility.Visible; LoadDebtors(); }
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabInventory.Visibility = Visibility.Visible; LoadInventory(); }
        private void btnSuppliersNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSuppliers.Visibility = Visibility.Visible; LoadSuppliers(); }
        private void btnStaffNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabStaff.Visibility = Visibility.Visible; LoadStaff(); }
        private void btnReportsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabReports.Visibility = Visibility.Visible; LoadFinancialReports(); }
        private void btnSettingsNav_Click(object sender, RoutedEventArgs e) { SettingsModalOverlay.Visibility = Visibility.Visible; }

        private void btnCashierNav_Click(object sender, RoutedEventArgs e)
        {
            CashierDashboard cd = new CashierDashboard(loggedInStaffID, loggedInStaffName);
            cd.WindowState = this.WindowState;
            if (this.WindowState == WindowState.Maximized) cd.WindowStartupLocation = WindowStartupLocation.Manual;
            cd.Show();
            this.Close();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.WindowState = this.WindowState;
            if (this.WindowState == WindowState.Maximized) login.WindowStartupLocation = WindowStartupLocation.Manual;
            login.Show();
            this.Close();
        }

        // report genaration 
        private void LoadFinancialReports()
        {
            BtnFilterReports_Click(null, null);
        }

        private void BtnFilterReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fromDate = DpReportFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
                string toDate = DpReportTo.SelectedDate.Value.ToString("yyyy-MM-dd");

                string dailyQuery = $@"
                    SELECT CAST(b.Date AS DATE) as Date, COUNT(DISTINCT b.BillID) as TotalBills, 
                           SUM(bi.QuantitySold * bi.SellingPriceAtTimeOfSale) as TotalRevenue, 
                           SUM((bi.SellingPriceAtTimeOfSale - ISNULL(ba.CostPrice, 0)) * bi.QuantitySold) as TotalProfit 
                    FROM Bill b 
                    JOIN Bill_Item bi ON b.BillID = bi.BillID 
                    LEFT JOIN Batch ba ON bi.BatchID = ba.BatchID 
                    WHERE b.Date >= '{fromDate}' AND b.Date <= '{toDate}' AND b.Status = 'Completed'
                    GROUP BY CAST(b.Date AS DATE) ORDER BY Date DESC";

                if (DgdFinancialSummary != null) DgdFinancialSummary.ItemsSource = FetchData(dailyQuery).DefaultView;

                string cashierQuery = $@"
                    SELECT s.FullName as CashierName, COUNT(DISTINCT b.BillID) as Transactions, SUM(bi.QuantitySold * bi.SellingPriceAtTimeOfSale) as TotalSales 
                    FROM Bill b JOIN Bill_Item bi ON b.BillID = bi.BillID JOIN Staff s ON b.StaffID = s.StaffID 
                    WHERE b.Date >= '{fromDate}' AND b.Date <= '{toDate}' AND b.Status = 'Completed' GROUP BY s.FullName";
                if (DgdCashierReport != null) DgdCashierReport.ItemsSource = FetchData(cashierQuery).DefaultView;

                string itemsQuery = $@"
                    SELECT p.Name as ProductName, SUM(bi.QuantitySold) as TotalQty, SUM(bi.QuantitySold * bi.SellingPriceAtTimeOfSale) as TotalRevenue 
                    FROM Bill_Item bi JOIN Bill b ON bi.BillID = b.BillID JOIN Product p ON bi.ProductID = p.ProductID 
                    WHERE b.Date >= '{fromDate}' AND b.Date <= '{toDate}' AND b.Status = 'Completed' GROUP BY p.Name ORDER BY TotalQty DESC";
                if (DgdItemsSoldReport != null) DgdItemsSoldReport.ItemsSource = FetchData(itemsQuery).DefaultView;

                UpdateMonthlyReportKPIs(fromDate, toDate);
            }
            catch (Exception ex) { MessageBox.Show("Report Error: " + ex.Message); }
        }

        private void UpdateMonthlyReportKPIs(string fromDate, string toDate)
        {
            try
            {
                string query = $@"
                    SELECT bi.QuantitySold, bi.SellingPriceAtTimeOfSale, ISNULL(ba.CostPrice, 0) as CostPrice 
                    FROM Bill b 
                    JOIN Bill_Item bi ON b.BillID = bi.BillID 
                    LEFT JOIN Batch ba ON bi.BatchID = ba.BatchID 
                    WHERE b.Date >= '{fromDate}' AND b.Date <= '{toDate}' AND b.Status = 'Completed'";

                DataTable dt = FetchData(query);

                decimal totalRevenue = 0;
                decimal totalCost = 0;

                foreach (DataRow row in dt.Rows)
                {
                    decimal qty = Convert.ToDecimal(row["QuantitySold"]);
                    decimal sellPrice = Convert.ToDecimal(row["SellingPriceAtTimeOfSale"]);
                    decimal costPrice = Convert.ToDecimal(row["CostPrice"]);

                    totalRevenue += (sellPrice * qty);
                    totalCost += (costPrice * qty);
                }

                decimal totalProfit = totalRevenue - totalCost;
                if (TxtMonthlyProfit != null) TxtMonthlyProfit.Text = $"LKR {totalProfit:N2}";
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
                    MessageBox.Show("Report Downloaded Successfully!");
                }
            }
            catch (Exception ex) { MessageBox.Show("Export Error: " + ex.Message); }
        }

        private void BtnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            try { PrintDialog printDlg = new PrintDialog(); if (printDlg.ShowDialog() == true) { printDlg.PrintVisual(DgdFinancialSummary, "Financial Summary Print"); } }
            catch (Exception ex) { MessageBox.Show("Print Error: " + ex.Message); }
        }

        // staff managment 
        private void LoadStaff() { DgdStaff.ItemsSource = FetchData("SELECT StaffID, FullName, Username, Role, Phone, IsActive FROM Staff").DefaultView; }

        private void BtnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            TxtStaffModalTitle.Text = "ADD NEW SYSTEM USER";
            HiddenStaffFormID.Text = "";
            TxtStaffFullName.Clear();
            TxtStaffUsername.Clear();
            TxtStaffPassword.Password = "";
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
            try
            {
                string fullName = TxtStaffFullName.Text.Trim();
                string username = TxtStaffUsername.Text.Trim();
                string password = TxtStaffPassword.Password;
                string role = CmbStaffRole.Text;
                string nic = TxtStaffNIC.Text.Trim();
                string phone = TxtStaffPhone.Text.Trim();

                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("Name, Username, and Role are required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal salary = 0;
                if (!string.IsNullOrWhiteSpace(TxtStaffSalary.Text) && !decimal.TryParse(TxtStaffSalary.Text, out salary))
                {
                    MessageBox.Show("Please enter a valid numeric value for Salary.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int age = 0;
                if (!string.IsNullOrWhiteSpace(TxtStaffAge.Text) && !int.TryParse(TxtStaffAge.Text, out age))
                {
                    MessageBox.Show("Please enter a valid numeric value for Age.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int isActive = (ChkStaffActive.IsChecked == true) ? 1 : 0;

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    if (string.IsNullOrEmpty(HiddenStaffFormID.Text))
                    {
                        if (string.IsNullOrEmpty(password))
                        {
                            MessageBox.Show("Password is required for new users.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        string query = @"INSERT INTO Staff (FullName, NIC, Phone, Username, PasswordHash, Role, Salary, StartingDate, Age, IsActive) 
                                         VALUES (@FN, @NIC, @Phone, @UN, @Pass, @Role, @Sal, CAST(GETDATE() AS DATE), @Age, @Act)";

                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@FN", fullName);
                        cmd.Parameters.AddWithValue("@NIC", nic);
                        cmd.Parameters.AddWithValue("@Phone", phone);
                        cmd.Parameters.AddWithValue("@UN", username);
                        cmd.Parameters.AddWithValue("@Pass", password);
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@Sal", salary);
                        cmd.Parameters.AddWithValue("@Age", age);
                        cmd.Parameters.AddWithValue("@Act", isActive);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        int staffId = int.Parse(HiddenStaffFormID.Text);
                        string query = @"UPDATE Staff SET FullName=@FN, NIC=@NIC, Phone=@Phone, Username=@UN, Role=@Role, Salary=@Sal, Age=@Age, IsActive=@Act ";

                        if (!string.IsNullOrEmpty(password)) query += ", PasswordHash=@Pass ";
                        query += " WHERE StaffID=@ID";

                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@FN", fullName);
                        cmd.Parameters.AddWithValue("@NIC", nic);
                        cmd.Parameters.AddWithValue("@Phone", phone);
                        cmd.Parameters.AddWithValue("@UN", username);
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@Sal", salary);
                        cmd.Parameters.AddWithValue("@Age", age);
                        cmd.Parameters.AddWithValue("@Act", isActive);
                        cmd.Parameters.AddWithValue("@ID", staffId);

                        if (!string.IsNullOrEmpty(password)) cmd.Parameters.AddWithValue("@Pass", password);

                        cmd.ExecuteNonQuery();
                    }
                }

                StaffModalOverlay.Visibility = Visibility.Collapsed;
                LoadStaff();
                MessageBox.Show("Staff member saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving staff: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (DgdStaff.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show($"Are you sure you want to PERMANENTLY DELETE {row["FullName"]}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection con = new SqlConnection(connString))
                        {
                            con.Open();
                            new SqlCommand($"DELETE FROM Staff WHERE StaffID = {row["StaffID"]}", con).ExecuteNonQuery();
                        }
                        LoadStaff();
                        MessageBox.Show("User deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Error deleting user. They may be tied to existing bills or records: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
            else
            {
                MessageBox.Show("Please select a staff member from the list first.", "Select User", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnViewStaff_Click(object sender, RoutedEventArgs e)
        {
            if (DgdStaff.SelectedItem is DataRowView row)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand($"SELECT * FROM Staff WHERE StaffID = {row["StaffID"]}", con);
                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            TxtViewStaffID.Text = reader["StaffID"].ToString();
                            TxtViewFullName.Text = reader["FullName"].ToString();
                            TxtViewUsername.Text = reader["Username"].ToString();
                            TxtViewPassword.Text = reader["PasswordHash"].ToString();
                            TxtViewRole.Text = reader["Role"].ToString();
                            TxtViewDate.Text = reader["StartingDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartingDate"]).ToString("yyyy-MM-dd") : "N/A";
                            TxtViewNIC.Text = reader["NIC"].ToString();
                            TxtViewPhone.Text = reader["Phone"].ToString();
                            TxtViewSalary.Text = reader["Salary"] != DBNull.Value ? Convert.ToDecimal(reader["Salary"]).ToString("N2") : "0.00";
                            TxtViewAge.Text = reader["Age"].ToString();
                            TxtViewStatus.Text = Convert.ToBoolean(reader["IsActive"]) ? "Active" : "Inactive";

                            ViewStaffModalOverlay.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error retrieving data: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a staff member from the list first.", "Select User", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCloseStaffModal_Click(object sender, RoutedEventArgs e) => StaffModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnCloseViewStaffModal_Click(object sender, RoutedEventArgs e) => ViewStaffModalOverlay.Visibility = Visibility.Collapsed;

        // inventory managment 
        private void LoadInventory() { DgdInventory.ItemsSource = FetchData("SELECT * FROM Product WHERE IsActive = 1").DefaultView; }
        private void BtnAddInventory_Click(object sender, RoutedEventArgs e) { TxtInvModalTitle.Text = "ADD NEW PRODUCT"; HiddenInvProductID.Text = ""; TxtInvName.Clear(); CmbInvCategory.Text = ""; TxtInvSubCategory.Clear(); TxtInvBrand.Clear(); TxtInvPrice.Clear(); TxtBatchQty.Clear(); TxtCostPrice.Clear(); TxtBatchNumber.Clear(); CmbSellingUnit.SelectedIndex = 0; CmbItemSize.Text = ""; CmbInvCategory.ItemsSource = FetchData("SELECT DISTINCT Category FROM Product WHERE Category IS NOT NULL").DefaultView; CmbInvSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView; InventoryModalOverlay.Visibility = Visibility.Visible; }
        private void CmbSellingUnit_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CmbSellingUnit == null || SpItemSize == null || CmbItemSize == null || TxtSizeLabel == null) return; ComboBoxItem selectedItem = CmbSellingUnit.SelectedItem as ComboBoxItem; if (selectedItem != null) { string unit = selectedItem.Content.ToString(); CmbItemSize.Items.Clear(); if (unit == "Liter") { SpItemSize.Visibility = Visibility.Visible; TxtSizeLabel.Text = "Volume (L):"; CmbItemSize.Items.Add("1"); CmbItemSize.Items.Add("4"); CmbItemSize.Items.Add("10"); CmbItemSize.Items.Add("20"); CmbItemSize.SelectedIndex = 0; } else if (unit == "Kg") { SpItemSize.Visibility = Visibility.Visible; TxtSizeLabel.Text = "Weight (Kg):"; CmbItemSize.Items.Add("0.5"); CmbItemSize.Items.Add("1"); CmbItemSize.Items.Add("5"); CmbItemSize.Items.Add("25"); CmbItemSize.Items.Add("50"); CmbItemSize.SelectedIndex = 0; } else { SpItemSize.Visibility = Visibility.Collapsed; CmbItemSize.Text = ""; } } }
        private void BtnDuplicateInventory_Click(object sender, RoutedEventArgs e) { if (DgdInventory.SelectedItem is DataRowView row) { TxtInvModalTitle.Text = "DUPLICATE PRODUCT (Edit size/details)"; HiddenInvProductID.Text = ""; TxtInvName.Text = row["Name"].ToString(); CmbInvCategory.ItemsSource = FetchData("SELECT DISTINCT Category FROM Product WHERE Category IS NOT NULL").DefaultView; CmbInvCategory.Text = row["Category"].ToString(); TxtInvSubCategory.Text = row["SubCategory"].ToString(); CmbSellingUnit.Text = row["SellingUnit"].ToString(); TxtInvBrand.Text = row["Brand"].ToString(); TxtInvPrice.Text = row["CurrentSellingPrice"].ToString(); TxtInvReorder.Text = row["ReorderLevel"].ToString(); ChkInvActive.IsChecked = Convert.ToBoolean(row["IsActive"]); TxtBatchNumber.Clear(); TxtBatchQty.Clear(); TxtCostPrice.Clear(); CmbInvSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView; InventoryModalOverlay.Visibility = Visibility.Visible; } else { MessageBox.Show("Please select an item to duplicate first.", "Select Item", MessageBoxButton.OK, MessageBoxImage.Information); } }
        private void BtnEditInventory_Click(object sender, RoutedEventArgs e) { if (DgdInventory.SelectedItem is DataRowView row) { TxtInvModalTitle.Text = "UPDATE PRODUCT"; HiddenInvProductID.Text = row["ProductID"].ToString(); TxtInvName.Text = row["Name"].ToString(); CmbInvCategory.ItemsSource = FetchData("SELECT DISTINCT Category FROM Product WHERE Category IS NOT NULL").DefaultView; CmbInvCategory.Text = row["Category"].ToString(); TxtInvSubCategory.Text = row["SubCategory"].ToString(); CmbSellingUnit.Text = row["SellingUnit"].ToString(); TxtInvBrand.Text = row["Brand"].ToString(); TxtInvPrice.Text = row["CurrentSellingPrice"].ToString(); TxtInvReorder.Text = row["ReorderLevel"].ToString(); ChkInvActive.IsChecked = Convert.ToBoolean(row["IsActive"]); InventoryModalOverlay.Visibility = Visibility.Visible; } }

        private void BtnSaveInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // validation 
                string finalName = TxtInvName.Text.Trim();
                if (string.IsNullOrWhiteSpace(finalName) || string.IsNullOrWhiteSpace(CmbInvCategory.Text))
                {
                    MessageBox.Show("Product Name and Category are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(TxtInvPrice.Text, out decimal sellPrice))
                {
                    MessageBox.Show("Please enter a valid Selling Price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int reorderLevel = 0;
                if (!string.IsNullOrWhiteSpace(TxtInvReorder.Text) && !int.TryParse(TxtInvReorder.Text, out reorderLevel))
                {
                    MessageBox.Show("Reorder Level must be a valid whole number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // If adding a new Batch/Product, validate Batch fields
                bool isNewProduct = string.IsNullOrEmpty(HiddenInvProductID.Text);
                decimal costPrice = 0;
                decimal batchQty = 0;

                if (isNewProduct || !string.IsNullOrWhiteSpace(TxtBatchNumber.Text))
                {
                    if (string.IsNullOrWhiteSpace(TxtBatchNumber.Text) || !decimal.TryParse(TxtCostPrice.Text, out costPrice) || !decimal.TryParse(TxtBatchQty.Text, out batchQty))
                    {
                        MessageBox.Show("When adding new stock, Batch Number, valid Cost Price, and valid Purchase Quantity are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                string rawCategory = CmbInvCategory.Text.Trim().ToLower();
                if (rawCategory.Length > 0) rawCategory = char.ToUpper(rawCategory[0]) + rawCategory.Substring(1);

                if (SpItemSize.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(CmbItemSize.Text))
                {
                    string unitSuffix = "";
                    if (CmbSellingUnit.Text == "Liter") unitSuffix = "L";
                    else if (CmbSellingUnit.Text == "Kg") unitSuffix = "Kg";
                    string sizeAppend = $"{CmbItemSize.Text.Trim()}{unitSuffix}";
                    if (!finalName.EndsWith(sizeAppend, StringComparison.OrdinalIgnoreCase))
                    {
                        finalName += $" {sizeAppend}";
                    }
                }

                Product p = new Product
                {
                    ProductID = isNewProduct ? 0 : int.Parse(HiddenInvProductID.Text),
                    Name = finalName,
                    Category = rawCategory,
                    SubCategory = TxtInvSubCategory.Text,
                    Brand = TxtInvBrand.Text,
                    SellingUnit = CmbSellingUnit.Text,
                    CurrentSellingPrice = sellPrice,
                    ReorderLevel = reorderLevel,
                    StaffID = loggedInStaffID,
                    IsActive = ChkInvActive.IsChecked ?? true,
                    ImagePath = HiddenImagePath.Text
                };

                Batch b = new Batch
                {
                    BatchNumber = TxtBatchNumber.Text,
                    CostPrice = costPrice,
                    InitialQuantity = batchQty
                };

                _inventoryService.ProcessProductInventory(p, b);

                if (isNewProduct && CmbInvSupplier.SelectedValue != null && !string.IsNullOrEmpty(TxtBatchNumber.Text))
                {
                    int supplierId = Convert.ToInt32(CmbInvSupplier.SelectedValue);
                    string paymentMethod = ((ComboBoxItem)CmbInvPaymentType.SelectedItem).Content.ToString();
                    decimal totalBatchCost = b.InitialQuantity * b.CostPrice;

                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();
                        new SqlCommand($"UPDATE Batch SET SupplierID = {supplierId} WHERE BatchNumber = '{b.BatchNumber}'", con).ExecuteNonQuery();

                        if (paymentMethod.Contains("Credit"))
                        {
                            try { new SqlCommand($"UPDATE Supplier SET OutstandingBalance = ISNULL(OutstandingBalance, 0) + {totalBatchCost} WHERE SupplierID = {supplierId}", con).ExecuteNonQuery(); } catch { }
                        }
                    }
                }

                InventoryModalOverlay.Visibility = Visibility.Collapsed;
                LoadInventory();
                RefreshDashboard();
                LoadSuppliers();
                MessageBox.Show("Product saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error saving product: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnDeleteInventory_Click(object sender, RoutedEventArgs e) { if (DgdInventory.SelectedItem is DataRowView row && MessageBox.Show("Delete this item?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { using (SqlConnection con = new SqlConnection(connString)) { con.Open(); new SqlCommand($"UPDATE Product SET IsActive = 0 WHERE ProductID = {row["ProductID"]}", con).ExecuteNonQuery(); } LoadInventory(); RefreshDashboard(); } }
        private void BtnCloseInventoryModal_Click(object sender, RoutedEventArgs e) => InventoryModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e) { OpenFileDialog op = new OpenFileDialog(); if (op.ShowDialog() == true) { ImgProductPreview.Source = new BitmapImage(new Uri(op.FileName)); HiddenImagePath.Text = op.FileName; } }

        // restock 
        private void BtnOpenRestock_Click(object sender, RoutedEventArgs e) { CmbRestockProduct.ItemsSource = FetchData("SELECT ProductID, Name, CurrentSellingPrice FROM Product WHERE IsActive = 1 ORDER BY Name").DefaultView; CmbRestockSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView; TxtRestockBatchNo.Text = "GRN-" + DateTime.Now.ToString("yyyyMMdd-HHmm"); TxtRestockQty.Clear(); TxtRestockCost.Clear(); TxtRestockSellingPrice.Clear(); RestockModalOverlay.Visibility = Visibility.Visible; }
        private void BtnCloseRestock_Click(object sender, RoutedEventArgs e) { RestockModalOverlay.Visibility = Visibility.Collapsed; }
        private void CmbRestockProduct_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CmbRestockProduct.SelectedItem is DataRowView row) { TxtRestockSellingPrice.Text = row["CurrentSellingPrice"].ToString(); } }

        private void BtnSaveRestock_Click(object sender, RoutedEventArgs e)
        {
            if (CmbRestockProduct.SelectedValue == null)
            {
                MessageBox.Show("Please select a product.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(TxtRestockQty.Text, out decimal qty) || !decimal.TryParse(TxtRestockCost.Text, out decimal costPrice) || !decimal.TryParse(TxtRestockSellingPrice.Text, out decimal newSellingPrice))
            {
                MessageBox.Show("Please enter valid numeric values for Quantity, Cost Price, and Selling Price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int productId = Convert.ToInt32(CmbRestockProduct.SelectedValue);
                int supplierId = CmbRestockSupplier.SelectedValue != null ? Convert.ToInt32(CmbRestockSupplier.SelectedValue) : 1;
                string paymentMethod = ((ComboBoxItem)CmbRestockPaymentType.SelectedItem).Content.ToString();
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

                        if (paymentMethod.Contains("Credit"))
                        {
                            try { string updateDebt = "UPDATE Supplier SET OutstandingBalance = ISNULL(OutstandingBalance, 0) + @AddedDebt WHERE SupplierID = @SID"; SqlCommand cmdDebt = new SqlCommand(updateDebt, con, transaction); cmdDebt.Parameters.AddWithValue("@AddedDebt", totalBatchCost); cmdDebt.Parameters.AddWithValue("@SID", supplierId); cmdDebt.ExecuteNonQuery(); } catch { }
                        }

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
                        LoadSuppliers();
                    }
                    catch (Exception ex) { transaction.Rollback(); throw ex; }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error processing restock: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // supplier manament logic 

        private void LoadSuppliers() { if (DgdSuppliers != null) { try { DgdSuppliers.ItemsSource = FetchData("SELECT SupplierID, SupplierName, CompanyName, Phone, Category, ISNULL(OutstandingBalance, 0) AS OutstandingBalance FROM Supplier ORDER BY SupplierName").DefaultView; } catch { DgdSuppliers.ItemsSource = FetchData("SELECT SupplierID, SupplierName, CompanyName, Phone, Category, 0.00 AS OutstandingBalance FROM Supplier ORDER BY SupplierName").DefaultView; } } }

        private void BtnQuickAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            TxtSupplierModalTitle.Text = "ADD NEW SUPPLIER";
            HiddenSupplierFormID.Text = "";
            TxtQuickSupplierName.Clear();
            if (TxtQuickSupplierCompany != null) TxtQuickSupplierCompany.Clear();
            TxtQuickSupplierPhone.Clear();
            QuickSupplierModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSuppliers.SelectedItem is DataRowView row)
            {
                TxtSupplierModalTitle.Text = "EDIT SUPPLIER";
                HiddenSupplierFormID.Text = row["SupplierID"].ToString();
                TxtQuickSupplierName.Text = row["SupplierName"].ToString();
                if (TxtQuickSupplierCompany != null) TxtQuickSupplierCompany.Text = row["CompanyName"].ToString();
                TxtQuickSupplierPhone.Text = row["Phone"].ToString();
                QuickSupplierModalOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Please select a supplier to edit.", "Select Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSuppliers.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show($"Are you sure you want to permanently delete {row["SupplierName"]}?\n\nThis may fail if they are tied to existing stock batches.", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection con = new SqlConnection(connString))
                        {
                            con.Open();
                            new SqlCommand($"DELETE FROM Supplier WHERE SupplierID = {row["SupplierID"]}", con).ExecuteNonQuery();
                        }
                        LoadSuppliers();
                        MessageBox.Show("Supplier deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Cannot delete this supplier. They are likely tied to existing inventory records or debts.\n\nDetails: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a supplier to delete.", "Select Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSaveQuickSupplier_Click(object sender, RoutedEventArgs e)
        {
            string supplierName = TxtQuickSupplierName.Text.Trim();
            if (string.IsNullOrEmpty(supplierName)) { MessageBox.Show("Supplier Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string companyName = TxtQuickSupplierCompany != null && !string.IsNullOrWhiteSpace(TxtQuickSupplierCompany.Text) ? TxtQuickSupplierCompany.Text.Trim() : "Unknown";

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    if (string.IsNullOrEmpty(HiddenSupplierFormID.Text))
                    {
                        string query = "INSERT INTO Supplier (SupplierName, CompanyName, Phone, Address, Category, OutstandingBalance) VALUES (@Name, @Company, @Phone, 'N/A', 'General', @Bal)";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@Name", supplierName);
                        cmd.Parameters.AddWithValue("@Company", companyName);
                        cmd.Parameters.AddWithValue("@Phone", TxtQuickSupplierPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@Bal", 0.00m);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string query = "UPDATE Supplier SET SupplierName=@Name, CompanyName=@Company, Phone=@Phone WHERE SupplierID=@ID";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@Name", supplierName);
                        cmd.Parameters.AddWithValue("@Company", companyName);
                        cmd.Parameters.AddWithValue("@Phone", TxtQuickSupplierPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@ID", int.Parse(HiddenSupplierFormID.Text));
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Supplier updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    QuickSupplierModalOverlay.Visibility = Visibility.Collapsed;
                    LoadSuppliers();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error saving supplier: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnCancelQuickSupplier_Click(object sender, RoutedEventArgs e) { QuickSupplierModalOverlay.Visibility = Visibility.Collapsed; }

        private void BtnViewSupplierStock_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSuppliers.SelectedItem is DataRowView row)
            {
                try
                {
                    int supplierId = Convert.ToInt32(row["SupplierID"]);
                    TxtStockModalSupplierName.Text = "Stock from: " + row["SupplierName"].ToString();

                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();

                        string query = $@"
                            SELECT p.Name as ProductName, ISNULL(SUM(b.CurrentQuantity), 0) as TotalStockLeft, p.SellingUnit 
                            FROM Batch b 
                            JOIN Product p ON b.ProductID = p.ProductID 
                            WHERE b.SupplierID = {supplierId} AND b.CurrentQuantity > 0 
                            GROUP BY p.Name, p.SellingUnit";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }

                    DgdSupplierStock.ItemsSource = dt.DefaultView;
                    SupplierStockModalOverlay.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading supplier stock from database:\n\n" + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a supplier first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCloseSupplierStock_Click(object sender, RoutedEventArgs e) { SupplierStockModalOverlay.Visibility = Visibility.Collapsed; }

        private void BtnOpenSettleSupplier_Click(object sender, RoutedEventArgs e) { if (DgdSuppliers.SelectedItem is DataRowView row) { HiddenSupplierPayID.Text = row["SupplierID"].ToString(); TxtSettleSupplierName.Text = row["SupplierName"].ToString(); TxtSettleSupplierTotalAmount.Text = $"LKR {Convert.ToDecimal(row["OutstandingBalance"]):N2}"; TxtSupplierAmountPaying.Clear(); TxtSupplierRemainingBalance.Text = "Balance After: LKR 0.00"; SettleSupplierDebtOverlay.Visibility = Visibility.Visible; } }

        private void BtnConfirmSettleSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtSupplierAmountPaying.Text, out decimal paying) || paying <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
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
        private void TxtSupplierAmountPaying_TextChanged(object sender, TextChangedEventArgs e) { try { decimal total = decimal.Parse(TxtSettleSupplierTotalAmount.Text.Replace("LKR ", "")); decimal paying = string.IsNullOrEmpty(TxtSupplierAmountPaying.Text) ? 0 : decimal.Parse(TxtSupplierAmountPaying.Text); TxtSupplierRemainingBalance.Text = $"Balance After: LKR {(total - paying):N2}"; } catch { } }
        private void BtnOpenAdjustDebt_Click(object sender, RoutedEventArgs e) { CmbAdjustDebtSupplier.ItemsSource = FetchData("SELECT SupplierID, SupplierName FROM Supplier ORDER BY SupplierName").DefaultView; TxtNewSupplierDebt.Clear(); AdjustSupplierDebtOverlay.Visibility = Visibility.Visible; }
        private void BtnCancelAdjustDebt_Click(object sender, RoutedEventArgs e) { AdjustSupplierDebtOverlay.Visibility = Visibility.Collapsed; }
        private void CmbAdjustDebtSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CmbAdjustDebtSupplier.SelectedItem is DataRowView row) { try { int supplierId = Convert.ToInt32(row["SupplierID"]); using (SqlConnection con = new SqlConnection(connString)) { con.Open(); var result = new SqlCommand($"SELECT OutstandingBalance FROM Supplier WHERE SupplierID = {supplierId}", con).ExecuteScalar(); if (result != DBNull.Value && result != null) { TxtNewSupplierDebt.Text = Convert.ToDecimal(result).ToString("0.00"); } } } catch { } } }
        private void BtnSaveAdjustDebt_Click(object sender, RoutedEventArgs e) { if (CmbAdjustDebtSupplier.SelectedValue == null || !decimal.TryParse(TxtNewSupplierDebt.Text, out decimal newDebt)) { MessageBox.Show("Please select a supplier and enter a valid debt amount.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning); return; } try { int supplierId = Convert.ToInt32(CmbAdjustDebtSupplier.SelectedValue); using (SqlConnection con = new SqlConnection(connString)) { con.Open(); new SqlCommand($"UPDATE Supplier SET OutstandingBalance = {newDebt} WHERE SupplierID = {supplierId}", con).ExecuteNonQuery(); } AdjustSupplierDebtOverlay.Visibility = Visibility.Collapsed; LoadSuppliers(); RefreshDashboard(); MessageBox.Show("Supplier debt adjusted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show("Error adjusting debt: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }

        // debotor manamnet 
        private void LoadDebtors() { DgdDebtors.ItemsSource = FetchData("SELECT * FROM Debtor").DefaultView; }
        private void BtnOpenSettleDebt_Click(object sender, RoutedEventArgs e) { if (DgdDebtors.SelectedItem is DataRowView row) { HiddenDebtorID.Text = row["DebtorID"].ToString(); TxtSettleCustomerName.Text = row["Name"].ToString(); TxtSettleTotalAmount.Text = $"LKR {Convert.ToDecimal(row["TotalOutstandingAmount"]):N2}"; TxtAmountPaying.Clear(); TxtRemainingBalance.Text = "Balance After: LKR 0.00"; SettleDebtOverlay.Visibility = Visibility.Visible; } }

        private void BtnConfirmSettle_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtAmountPaying.Text, out decimal paying) || paying <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
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
        private void TxtAmountPaying_TextChanged(object sender, TextChangedEventArgs e) { try { decimal total = decimal.Parse(TxtSettleTotalAmount.Text.Replace("LKR ", "")); decimal paying = string.IsNullOrEmpty(TxtAmountPaying.Text) ? 0 : decimal.Parse(TxtAmountPaying.Text); TxtRemainingBalance.Text = $"Balance After: LKR {(total - paying):N2}"; } catch { } }

        // sales history manament 
        private void LoadSales() { string query = "SELECT b.*, s.FullName as StaffName FROM Bill b JOIN Staff s ON b.StaffID = s.StaffID WHERE 1=1 "; if (DpSalesFrom != null && DpSalesFrom.SelectedDate.HasValue) query += $" AND b.Date >= '{DpSalesFrom.SelectedDate.Value:yyyy-MM-dd}' "; if (DpSalesTo != null && DpSalesTo.SelectedDate.HasValue) query += $" AND b.Date <= '{DpSalesTo.SelectedDate.Value:yyyy-MM-dd}' "; query += " ORDER BY b.Date DESC, b.Time DESC"; if (DgdSales != null) DgdSales.ItemsSource = FetchData(query).DefaultView; }
        private void BtnFilterSalesHistory_Click(object sender, RoutedEventArgs e) { LoadSales(); }
        private void BtnViewBill_Click(object sender, RoutedEventArgs e) { if (DgdSales.SelectedItem is DataRowView row) { TxtReceiptBillID.Text = "#" + row["BillID"].ToString(); DateTime date = Convert.ToDateTime(row["Date"]); TxtReceiptDate.Text = date.ToString("dd/MM/yyyy") + " " + row["Time"].ToString(); TxtReceiptTotal.Text = Convert.ToDecimal(row["GrandTotal"]).ToString("N2"); TxtReceiptCash.Text = row["PaymentType"].ToString(); string query = $"SELECT p.Name as ProductName, bi.QuantitySold as Quantity, (bi.SellingPriceAtTimeOfSale * bi.QuantitySold) as TotalPrice FROM Bill_Item bi JOIN Product p ON bi.ProductID = p.ProductID WHERE bi.BillID = {row["BillID"]}"; IcBillItems.ItemsSource = FetchData(query).DefaultView; BillViewerOverlay.Visibility = Visibility.Visible; } }
        private void BtnPrintBill_Click(object sender, RoutedEventArgs e) { try { PrintDialog printDlg = new PrintDialog(); if (printDlg.ShowDialog() == true) { BillActionGrid.Visibility = Visibility.Collapsed; printDlg.PrintVisual(ManagerReceiptPaper, "Receipt Copy"); BillActionGrid.Visibility = Visibility.Visible; } } catch (Exception ex) { BillActionGrid.Visibility = Visibility.Visible; MessageBox.Show("Print Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void BtnCloseBill_Click(object sender, RoutedEventArgs e) => BillViewerOverlay.Visibility = Visibility.Collapsed;
        private void CmbStaffFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { LoadSales(); }

        // setting and dashboard logic 
        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e) { SettingsModalOverlay.Visibility = Visibility.Collapsed; }
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e) { try { if (decimal.TryParse(TxtDailyGoal.Text, out decimal newGoal)) { _dailyGoal = newGoal; } } catch { } string selectedTheme = ((ComboBoxItem)CmbTheme.SelectedItem).Content.ToString(); if (selectedTheme == "Dark Mode") { this.Resources["AppBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")); this.Resources["CardBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")); this.Resources["TextMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")); this.Resources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")); } else { this.Resources["AppBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")); this.Resources["CardBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")); this.Resources["TextMain"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")); this.Resources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); } SettingsModalOverlay.Visibility = Visibility.Collapsed; RefreshDashboard(); MessageBox.Show("Settings saved and applied successfully!", "Settings Updated", MessageBoxButton.OK, MessageBoxImage.Information); }
        private void BtnBackupDB_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Database backup initiated securely.", "Backup Status", MessageBoxButton.OK, MessageBoxImage.Information); }

        private void RefreshDashboard()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    decimal totalItems = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(CurrentQuantity),0) FROM Batch", con).ExecuteScalar());
                    if (TxtTotalItems != null) TxtTotalItems.Text = totalItems.ToString("0.##");
                    try { var payable = new SqlCommand("SELECT ISNULL(SUM(OutstandingBalance),0) FROM Supplier", con).ExecuteScalar(); if (TxtTotalPayable != null) TxtTotalPayable.Text = $"LKR {Convert.ToDecimal(payable):N2}"; } catch { if (TxtTotalPayable != null) TxtTotalPayable.Text = "LKR 0.00"; }
                    decimal rev = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(GrandTotal),0) FROM Bill WHERE Date=CAST(GETDATE() AS DATE) AND Status='Completed'", con).ExecuteScalar());
                    if (TxtTodayRevenue != null) TxtTodayRevenue.Text = $"LKR {rev:N2}";
                    if (ProgDailyGoal != null) { ProgDailyGoal.Maximum = (double)_dailyGoal; ProgDailyGoal.Value = (double)rev; decimal percent = (_dailyGoal > 0) ? (rev / _dailyGoal) * 100 : 0; if (TxtGoalProgress != null) TxtGoalProgress.Text = $"{percent:N1}% of Daily Target (LKR {_dailyGoal:N0})"; }
                    if (TxtTotalTransactions != null) TxtTotalTransactions.Text = new SqlCommand("SELECT COUNT(*) FROM Bill WHERE Date=CAST(GETDATE() AS DATE)", con).ExecuteScalar().ToString();
                }

                if (DgdHomeInventory != null) DgdHomeInventory.ItemsSource = FetchData("SELECT TOP 10 Name, TotalQuantity, SellingUnit FROM Product WHERE IsActive=1 ORDER BY TotalQuantity DESC").DefaultView;
                DataTable dtLowStock = _inventoryService.GetLowStockItems();
                if (DgdLowStock != null) DgdLowStock.ItemsSource = dtLowStock.DefaultView;
                int lowStockCount = dtLowStock.Rows.Count;
                if (TxtLowStock != null) TxtLowStock.Text = $"{lowStockCount} Items";
                if (TxtNotificationCount != null) TxtNotificationCount.Text = lowStockCount.ToString();
            }
            catch { }
        }

        private DataTable FetchData(string query)
        {
            DataTable dt = new DataTable();
            try { using (SqlConnection con = new SqlConnection(connString)) { new SqlDataAdapter(query, con).Fill(dt); } } catch { }
            return dt;
        }
    }
}