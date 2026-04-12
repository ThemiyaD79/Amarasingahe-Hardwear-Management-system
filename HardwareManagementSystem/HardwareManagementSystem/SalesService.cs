using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class SalesService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public int ExecuteTransaction(List<CartItem> cart, int staffId, decimal total, string paymentType, int? debtorId)
        {
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    string billSql = "INSERT INTO Bill (Date, Time, SubTotal, BillDiscount, GrandTotal, PaymentType, Status, StaffID, DebtorID) VALUES (GETDATE(), CAST(GETDATE() AS TIME), @total, 0, @total, @type, 'Completed', @sid, @did); SELECT SCOPE_IDENTITY();";
                    SqlCommand bCmd = new SqlCommand(billSql, con, trans);
                    bCmd.Parameters.AddWithValue("@total", total);
                    bCmd.Parameters.AddWithValue("@type", paymentType);
                    bCmd.Parameters.AddWithValue("@sid", staffId);
                    bCmd.Parameters.AddWithValue("@did", (object)debtorId ?? DBNull.Value);
                    int billId = Convert.ToInt32(bCmd.ExecuteScalar());

                    foreach (var item in cart)
                    {
                        string bSql = "SELECT TOP 1 BatchID FROM Batch WHERE ProductID = @pid AND CurrentQuantity >= @qty ORDER BY ManufacturingDate ASC";
                        SqlCommand bCmdFind = new SqlCommand(bSql, con, trans);
                        bCmdFind.Parameters.AddWithValue("@pid", item.ProductID);
                        bCmdFind.Parameters.AddWithValue("@qty", item.Quantity);
                        int batchId = Convert.ToInt32(bCmdFind.ExecuteScalar());

                        string iSql = "INSERT INTO Bill_Item (QuantitySold, LineDiscount, SellingPriceAtTimeOfSale, BillID, ProductID, BatchID) VALUES (@qty, 0, @price, @bid, @pid, @baid)";
                        SqlCommand iCmd = new SqlCommand(iSql, con, trans);
                        iCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        iCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                        iCmd.Parameters.AddWithValue("@bid", billId);
                        iCmd.Parameters.AddWithValue("@pid", item.ProductID);
                        iCmd.Parameters.AddWithValue("@baid", batchId);
                        iCmd.ExecuteNonQuery();

                        new SqlCommand($"UPDATE Batch SET CurrentQuantity -= {item.Quantity} WHERE BatchID = {batchId}", con, trans).ExecuteNonQuery();
                    }
                    trans.Commit(); return billId;
                }
                catch { trans.Rollback(); throw; }
            }
        }
    }
}