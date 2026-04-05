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
            if (TxtWelcomeMessage != null) TxtWelcomeMessage.Text = $"Welcome Back, {loggedInStaffName}!";
            RefreshDashboard();
        }

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
        private void btnCashierNav_Click(object sender, RoutedEventArgs e) { new CashierDashboard().Show(); this.Close(); }
        private void btnLogout_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }

        // ==========================================
        // 1. INVENTORY CRUD (NOW WORKING)
        // ==========================================
        private void LoadInventory() { DgdInventory.ItemsSource = FetchData("SELECT * FROM Product WHERE IsActive = 1").DefaultView; }

        private void BtnAddInventory_Click(object sender, RoutedEventArgs e) { TxtInvModalTitle.Text = "ADD PRODUCT"; HiddenInvProductID.Text = ""; InventoryModalOverlay.Visibility = Visibility.Visible; }

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
                    string sql = string.IsNullOrEmpty(HiddenInvProductID.Text) ?
                        "INSERT INTO Product (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID) VALUES (@n,@c,@b,@p,@q,@r,@a,@s)" :
                        "UPDATE Product SET Name=@n, Category=@c, Brand=@b, CurrentSellingPrice=@p, ReorderLevel=@r, IsActive=@a WHERE ProductID=@id";

                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@n", TxtInvName.Text);
                    cmd.Parameters.AddWithValue("@c", TxtInvCategory.Text);
                    cmd.Parameters.AddWithValue("@b", TxtInvBrand.Text);
                    cmd.Parameters.AddWithValue("@p", decimal.Parse(TxtInvPrice.Text));
                    cmd.Parameters.AddWithValue("@r", int.Parse(TxtInvReorder.Text));
                    cmd.Parameters.AddWithValue("@a", ChkInvActive.IsChecked);

                    if (string.IsNullOrEmpty(HiddenInvProductID.Text))
                    {
                        cmd.Parameters.AddWithValue("@q", int.Parse(TxtBatchQty.Text));
                        cmd.Parameters.AddWithValue("@s", loggedInStaffID);
                    }
                    else { cmd.Parameters.AddWithValue("@id", HiddenInvProductID.Text); }

                    cmd.ExecuteNonQuery();
                }
                InventoryModalOverlay.Visibility = Visibility.Collapsed; LoadInventory();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnDeleteInventory_Click(object sender, RoutedEventArgs e)
        {
            if (DgdInventory.SelectedItem is DataRowView row && MessageBox.Show("Delete item?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExecuteNonQuery($"UPDATE Product SET IsActive = 0 WHERE ProductID = {row["ProductID"]}");
                LoadInventory();
            }
        }

        // ==========================================
        // 2. STAFF CRUD (NOW WORKING)
        // ==========================================
        private void LoadStaff() { DgdStaff.ItemsSource = FetchData("SELECT * FROM Staff WHERE IsActive = 1").DefaultView; }

        private void BtnAddStaff_Click(object sender, RoutedEventArgs e) { TxtStaffModalTitle.Text = "ADD STAFF"; HiddenStaffFormID.Text = ""; StaffModalOverlay.Visibility = Visibility.Visible; }

        private void BtnEditStaff_Click(object sender, RoutedEventArgs e)
        {
            if (DgdStaff.SelectedItem is DataRowView row)
            {
                TxtStaffModalTitle.Text = "EDIT STAFF";
                HiddenStaffFormID.Text = row["StaffID"].ToString();
                TxtStaffFullName.Text = row["FullName"].ToString();
                TxtStaffUsername.Text = row["Username"].ToString();
                CmbStaffRole.Text = row["Role"].ToString();
                StaffModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveStaff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    string sql = string.IsNullOrEmpty(HiddenStaffFormID.Text) ?
                        "INSERT INTO Staff (FullName, NIC, Phone, Username, PasswordHash, Role, Salary, StartingDate, Age, IsActive) VALUES (@fn,@nic,@ph,@un,@pw,@rl,@sal,GETDATE(),@age,@ac)" :
                        "UPDATE Staff SET FullName=@fn, Username=@un, Role=@rl, IsActive=@ac WHERE StaffID=@id";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@fn", TxtStaffFullName.Text);
                    cmd.Parameters.AddWithValue("@un", TxtStaffUsername.Text);
                    cmd.Parameters.AddWithValue("@rl", CmbStaffRole.Text);
                    cmd.Parameters.AddWithValue("@ac", ChkStaffActive.IsChecked);
                    if (!string.IsNullOrEmpty(HiddenStaffFormID.Text)) cmd.Parameters.AddWithValue("@id", HiddenStaffFormID.Text);
                    else { cmd.Parameters.AddWithValue("@nic", TxtStaffNIC.Text); cmd.Parameters.AddWithValue("@ph", TxtStaffPhone.Text); cmd.Parameters.AddWithValue("@pw", TxtStaffPassword.Password); cmd.Parameters.AddWithValue("@sal", decimal.Parse(TxtStaffSalary.Text)); cmd.Parameters.AddWithValue("@age", int.Parse(TxtStaffAge.Text)); }
                    cmd.ExecuteNonQuery();
                }
                StaffModalOverlay.Visibility = Visibility.Collapsed; LoadStaff();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnDeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (DgdStaff.SelectedItem is DataRowView row && MessageBox.Show("Delete user?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExecuteNonQuery($"UPDATE Staff SET IsActive = 0 WHERE StaffID = {row["StaffID"]}");
                LoadStaff();
            }
        }

        // ==========================================
        // 3. UTILS & HELPERS
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
                    var rev = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Bill WHERE Date = CAST(GETDATE() AS DATE) AND Status = 'Completed'", con).ExecuteScalar();
                    TxtTodayRevenue.Text = $"LKR {Convert.ToDecimal(rev):N2}";
                    TxtTotalTransactions.Text = new SqlCommand("SELECT COUNT(*) FROM Bill WHERE Date = CAST(GETDATE() AS DATE)", con).ExecuteScalar().ToString();
                }
            }
            catch { }
        }

        private DataTable FetchData(string q) { DataTable dt = new DataTable(); using (SqlConnection con = new SqlConnection(connString)) { new SqlDataAdapter(q, con).Fill(dt); } return dt; }
        private void ExecuteNonQuery(string s) { using (SqlConnection con = new SqlConnection(connString)) { con.Open(); new SqlCommand(s, con).ExecuteNonQuery(); } }
        private void LoadSales() { DgdSales.ItemsSource = FetchData("SELECT b.*, s.FullName as StaffName FROM Bill b JOIN Staff s ON b.StaffID = s.StaffID").DefaultView; }
        private void LoadDebtors() { DgdDebtors.ItemsSource = FetchData("SELECT * FROM Debtor WHERE TotalOutstandingAmount > 0").DefaultView; }
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e) { OpenFileDialog op = new OpenFileDialog(); if (op.ShowDialog() == true) { ImgProductPreview.Source = new BitmapImage(new Uri(op.FileName)); HiddenImagePath.Text = op.FileName; } }
        private void BtnCloseInventoryModal_Click(object sender, RoutedEventArgs e) => InventoryModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnCloseStaffModal_Click(object sender, RoutedEventArgs e) => StaffModalOverlay.Visibility = Visibility.Collapsed;
        private void BtnOpenSettleDebt_Click(object sender, RoutedEventArgs e) { } // Placeholder
        private void BtnViewBill_Click(object sender, RoutedEventArgs e) { } // Placeholder
        private void CmbStaffFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}