using System;
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

            RefreshDashboard();
        }

        // ==========================================
        // NAVIGATION
        // ==========================================
        private void HideAllTabs() { TabOverview.Visibility = TabDebtors.Visibility = TabSales.Visibility = TabStaff.Visibility = TabInventory.Visibility = Visibility.Collapsed; }
        private void btnHomeNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabOverview.Visibility = Visibility.Visible; RefreshDashboard(); }
        private void btnSalesNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSales.Visibility = Visibility.Visible; LoadSales(); }
        private void btnDebtorsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabDebtors.Visibility = Visibility.Visible; LoadDebtors(); }
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabInventory.Visibility = Visibility.Visible; LoadInventory(); }
        private void btnStaffNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabStaff.Visibility = Visibility.Visible; LoadStaff(); }
        private void btnCashierNav_Click(object sender, RoutedEventArgs e) { new CashierDashboard(loggedInStaffID, loggedInStaffName).Show(); this.Close(); }
        private void btnLogout_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }

        // ==========================================
        // INVENTORY LOGIC (Updated for SubCategory, SellingUnit, and Decimals)
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
                    InitialQuantity = string.IsNullOrEmpty(TxtBatchQty.Text) ? 0 : decimal.Parse(TxtBatchQty.Text) // Now using Decimal!
                };

                _inventoryService.ProcessProductInventory(p, b);
                InventoryModalOverlay.Visibility = Visibility.Collapsed;
                LoadInventory();
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
            }
        }

        private void BtnCloseInventoryModal_Click(object sender, RoutedEventArgs e) => InventoryModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e) { OpenFileDialog op = new OpenFileDialog(); if (op.ShowDialog() == true) { ImgProductPreview.Source = new BitmapImage(new Uri(op.FileName)); HiddenImagePath.Text = op.FileName; } }

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
                TxtStaffNIC.Text = "Hidden"; // Don't expose sensitive info here typically
                TxtStaffPhone.Text = row["Phone"].ToString();
                ChkStaffActive.IsChecked = Convert.ToBoolean(row["IsActive"]);
                StaffModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveStaff_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder logic to close modal. Real SQL logic was provided in previous turn.
            StaffModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnDeleteStaff_Click(object sender, RoutedEventArgs e) { }
        private void BtnCloseStaffModal_Click(object sender, RoutedEventArgs e) => StaffModalOverlay.Visibility = Visibility.Collapsed;

        // ==========================================
        // HELPERS
        // ==========================================
        private void RefreshDashboard()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    TxtTotalStaff.Text = new SqlCommand("SELECT COUNT(*) FROM Staff WHERE IsActive=1", con).ExecuteScalar().ToString();
                    TxtTotalItems.Text = new SqlCommand("SELECT ISNULL(SUM(CurrentQuantity),0) FROM Batch", con).ExecuteScalar().ToString();
                    var rev = new SqlCommand("SELECT ISNULL(SUM(GrandTotal),0) FROM Bill WHERE Date=CAST(GETDATE() AS DATE) AND Status='Completed'", con).ExecuteScalar();
                    TxtTodayRevenue.Text = $"LKR {Convert.ToDecimal(rev):N2}";
                    TxtTotalTransactions.Text = new SqlCommand("SELECT COUNT(*) FROM Bill WHERE Date=CAST(GETDATE() AS DATE)", con).ExecuteScalar().ToString();
                }
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