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

        public MainDashboard(int staffId = 2, string staffName = "Manager")
        {
            InitializeComponent();
            this.loggedInStaffID = staffId;
            this.loggedInStaffName = staffName;
            if (TxtWelcomeMessage != null)
                TxtWelcomeMessage.Text = $"Welcome Back, {loggedInStaffName}!";
            RefreshDashboard();
        }

        // ==========================================
        // 1. NAVIGATION LOGIC
        // ==========================================
        private void HideAllTabs()
        {
            TabOverview.Visibility = TabDebtors.Visibility = TabSales.Visibility =
            TabStaff.Visibility = TabInventory.Visibility = Visibility.Collapsed;
        }

        private void btnHomeNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabOverview.Visibility = Visibility.Visible; RefreshDashboard(); }
        private void btnSalesNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabSales.Visibility = Visibility.Visible; LoadSales(); }
        private void btnDebtorsNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabDebtors.Visibility = Visibility.Visible; LoadDebtors(); }
        private void btnInventoryNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabInventory.Visibility = Visibility.Visible; LoadInventory(); }
        private void btnStaffNav_Click(object sender, RoutedEventArgs e) { HideAllTabs(); TabStaff.Visibility = Visibility.Visible; LoadStaff(); }
        private void btnCashierNav_Click(object sender, RoutedEventArgs e) { new CashierDashboard(loggedInStaffID, loggedInStaffName).Show(); this.Close(); }
        private void btnLogout_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }

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

        // ==========================================
        // 2. INVENTORY CRUD (Update/Delete/Add)
        // ==========================================
        private void LoadInventory() { DgdInventory.ItemsSource = FetchData("SELECT * FROM Product WHERE IsActive = 1").DefaultView; }

        private void BtnAddInventory_Click(object sender, RoutedEventArgs e)
        {
            TxtInvModalTitle.Text = "ADD NEW PRODUCT";
            HiddenInvProductID.Text = "";
            TxtInvName.Clear(); TxtInvCategory.Clear(); TxtInvBrand.Clear(); TxtInvPrice.Clear(); TxtInvReorder.Clear();
            InventoryModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnEditInventory_Click(object sender, RoutedEventArgs e)
        {
            if (DgdInventory.SelectedItem is DataRowView row)
            {
                TxtInvModalTitle.Text = "UPDATE PRODUCT";
                HiddenInvProductID.Text = row["ProductID"].ToString();
                TxtInvName.Text = row["Name"].ToString();
                TxtInvPrice.Text = row["CurrentSellingPrice"].ToString();
                TxtInvCategory.Text = row["Category"].ToString();
                TxtInvBrand.Text = row["Brand"].ToString();
                TxtInvReorder.Text = row["ReorderLevel"].ToString();
                ChkInvActive.IsChecked = Convert.ToBoolean(row["IsActive"]);
                InventoryModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    SqlCommand cmd;
                    if (!string.IsNullOrEmpty(HiddenInvProductID.Text))
                    {
                        cmd = new SqlCommand("UPDATE Product SET Name=@n, Category=@c, Brand=@b, CurrentSellingPrice=@p, ReorderLevel=@r, IsActive=@a, ImagePath=@img WHERE ProductID=@id", con);
                        cmd.Parameters.AddWithValue("@id", HiddenInvProductID.Text);
                    }
                    else
                    {
                        cmd = new SqlCommand(@"INSERT INTO Product (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID, ImagePath) 
                                             VALUES (@n, @c, @b, @p, @q, @r, @a, @s, @img); SELECT SCOPE_IDENTITY();", con);
                        cmd.Parameters.AddWithValue("@s", loggedInStaffID);
                        cmd.Parameters.AddWithValue("@q", TxtBatchQty.Text);
                    }
                    cmd.Parameters.AddWithValue("@n", TxtInvName.Text); cmd.Parameters.AddWithValue("@c", TxtInvCategory.Text);
                    cmd.Parameters.AddWithValue("@b", TxtInvBrand.Text); cmd.Parameters.AddWithValue("@p", decimal.Parse(TxtInvPrice.Text));
                    cmd.Parameters.AddWithValue("@r", int.Parse(TxtInvReorder.Text)); cmd.Parameters.AddWithValue("@a", ChkInvActive.IsChecked);
                    cmd.Parameters.AddWithValue("@img", HiddenImagePath.Text ?? "");

                    if (string.IsNullOrEmpty(HiddenInvProductID.Text))
                    {
                        int newID = Convert.ToInt32(cmd.ExecuteScalar());
                        SqlCommand bCmd = new SqlCommand("INSERT INTO Batch (BatchNumber, CostPrice, InitialQuantity, CurrentQuantity, ManufacturingDate, ProductID, SupplierID) VALUES (@bn,@cp,@iq,@iq,GETDATE(),@pid,1)", con);
                        bCmd.Parameters.AddWithValue("@bn", TxtBatchNumber.Text); bCmd.Parameters.AddWithValue("@cp", TxtCostPrice.Text);
                        bCmd.Parameters.AddWithValue("@iq", TxtBatchQty.Text); bCmd.Parameters.AddWithValue("@pid", newID);
                        bCmd.ExecuteNonQuery();
                    }
                    else { cmd.ExecuteNonQuery(); }
                }
                InventoryModalOverlay.Visibility = Visibility.Collapsed; LoadInventory();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
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

        // ==========================================
        // 3. DEBTOR LOGIC (Fixing Pay Now & Receipt)
        // ==========================================
        private void LoadDebtors() { DgdDebtors.ItemsSource = FetchData("SELECT * FROM Debtor WHERE TotalOutstandingAmount > 0").DefaultView; }

        private void BtnOpenSettleDebt_Click(object sender, RoutedEventArgs e)
        {
            if (DgdDebtors.SelectedItem is DataRowView row)
            {
                HiddenDebtorID.Text = row["DebtorID"].ToString();
                TxtSettleCustomerName.Text = row["Name"].ToString();
                TxtSettleTotalAmount.Text = $"LKR {Convert.ToDecimal(row["TotalOutstandingAmount"]):N2}";
                TxtAmountPaying.Clear();
                SettleDebtOverlay.Visibility = Visibility.Visible;
            }
        }

        private void TxtAmountPaying_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal total = decimal.Parse(TxtSettleTotalAmount.Text.Replace("LKR ", "").Replace(",", ""));
                decimal paying = string.IsNullOrEmpty(TxtAmountPaying.Text) ? 0 : decimal.Parse(TxtAmountPaying.Text);
                TxtRemainingBalance.Text = $"Balance After: LKR {(total - paying):N2}";
            }
            catch { }
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
                MessageBox.Show("Payment recorded successfully.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnCancelSettle_Click(object sender, RoutedEventArgs e) => SettleDebtOverlay.Visibility = Visibility.Collapsed;

        // ==========================================
        // 4. SALES & STAFF LOGIC
        // ==========================================
        private void LoadSales() { DgdSales.ItemsSource = FetchData("SELECT b.*, s.FullName as StaffName FROM Bill b JOIN Staff s ON b.StaffID = s.StaffID").DefaultView; }

        private void BtnViewBill_Click(object sender, RoutedEventArgs e)
        {
            if (DgdSales.SelectedItem is DataRowView row)
            {
                TxtReceiptBillID.Text = "#" + row["BillID"];
                TxtReceiptDate.Text = Convert.ToDateTime(row["Date"]).ToString("dd/MM/yyyy");
                TxtReceiptTotal.Text = Convert.ToDecimal(row["GrandTotal"]).ToString("N2");
                TxtReceiptCash.Text = row["PaymentType"].ToString();
                IcBillItems.ItemsSource = FetchData($"SELECT p.Name as ProductName, bi.QuantitySold as Quantity, (bi.SellingPriceAtTimeOfSale * bi.QuantitySold) as TotalPrice FROM Bill_Item bi JOIN Product p ON bi.ProductID = p.ProductID WHERE bi.BillID = {row["BillID"]}").DefaultView;
                BillViewerOverlay.Visibility = Visibility.Visible;
            }
        }

        private void LoadStaff() { DgdStaff.ItemsSource = FetchData("SELECT * FROM Staff WHERE IsActive = 1").DefaultView; }
        private void BtnAddStaff_Click(object sender, RoutedEventArgs e) { TxtStaffModalTitle.Text = "ADD STAFF"; HiddenStaffFormID.Text = ""; StaffModalOverlay.Visibility = Visibility.Visible; }
        private void BtnSaveStaff_Click(object sender, RoutedEventArgs e) { /* Existing Staff Logic */ StaffModalOverlay.Visibility = Visibility.Collapsed; LoadStaff(); }
        private void BtnEditStaff_Click(object sender, RoutedEventArgs e) { if (DgdStaff.SelectedItem is DataRowView row) { HiddenStaffFormID.Text = row["StaffID"].ToString(); TxtStaffFullName.Text = row["FullName"].ToString(); StaffModalOverlay.Visibility = Visibility.Visible; } }
        private void BtnDeleteStaff_Click(object sender, RoutedEventArgs e) { if (DgdStaff.SelectedItem is DataRowView row) { using (SqlConnection con = new SqlConnection(connString)) { con.Open(); new SqlCommand($"UPDATE Staff SET IsActive=0 WHERE StaffID={row["StaffID"]}", con).ExecuteNonQuery(); } LoadStaff(); } }

        // ==========================================
        // HELPERS
        // ==========================================
        private DataTable FetchData(string query) { DataTable dt = new DataTable(); using (SqlConnection con = new SqlConnection(connString)) { new SqlDataAdapter(query, con).Fill(dt); } return dt; }
        private void BtnCloseInventoryModal_Click(object sender, RoutedEventArgs e) => InventoryModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnCloseStaffModal_Click(object sender, RoutedEventArgs e) => StaffModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnCloseBill_Click(object sender, RoutedEventArgs e) => BillViewerOverlay.Visibility = Visibility.Collapsed;
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e) { OpenFileDialog op = new OpenFileDialog(); if (op.ShowDialog() == true) { ImgProductPreview.Source = new BitmapImage(new Uri(op.FileName)); HiddenImagePath.Text = op.FileName; } }
        private void CmbStaffFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}