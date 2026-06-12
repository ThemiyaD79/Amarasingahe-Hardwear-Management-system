using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class SalesService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public int ExecuteTransaction(List<CartItem> cart, int staffId, decimal total, string paymentType, int? debtorId)
        {
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                con.Open();
                SQLiteTransaction trans = con.BeginTransaction();
                try
                {
                    string billSql = "INSERT INTO Bill (Date, Time, SubTotal, BillDiscount, GrandTotal, PaymentType, Status, StaffID, DebtorID) VALUES (CURRENT_DATE, CURRENT_TIME, @total, 0, @total, @type, 'Completed', @sid, @did); SELECT last_insert_rowid();";
                    SQLiteCommand bCmd = new SQLiteCommand(billSql, con, trans);
                    bCmd.Parameters.AddWithValue("@total", total);
                    bCmd.Parameters.AddWithValue("@type", paymentType);
                    bCmd.Parameters.AddWithValue("@sid", staffId);
                    bCmd.Parameters.AddWithValue("@did", (object)debtorId ?? DBNull.Value);
                    int billId = Convert.ToInt32(bCmd.ExecuteScalar());

                    foreach (var item in cart)
                    {
                        string bSql = "SELECT BatchID FROM Batch WHERE ProductID = @pid AND CurrentQuantity >= @qty ORDER BY ManufacturingDate ASC LIMIT 1";
                        SQLiteCommand bCmdFind = new SQLiteCommand(bSql, con, trans);
                        bCmdFind.Parameters.AddWithValue("@pid", item.ProductID);
                        bCmdFind.Parameters.AddWithValue("@qty", item.Quantity);
                        int batchId = Convert.ToInt32(bCmdFind.ExecuteScalar());

                        string iSql = "INSERT INTO Bill_Item (QuantitySold, LineDiscount, SellingPriceAtTimeOfSale, BillID, ProductID, BatchID) VALUES (@qty, 0, @price, @bid, @pid, @baid)";
                        SQLiteCommand iCmd = new SQLiteCommand(iSql, con, trans);
                        iCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        iCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                        iCmd.Parameters.AddWithValue("@bid", billId);
                        iCmd.Parameters.AddWithValue("@pid", item.ProductID);
                        iCmd.Parameters.AddWithValue("@baid", batchId);
                        iCmd.ExecuteNonQuery();

                        new SQLiteCommand($"UPDATE Batch SET CurrentQuantity = CurrentQuantity - {item.Quantity} WHERE BatchID = {batchId}", con, trans).ExecuteNonQuery();
                    }
                    trans.Commit(); return billId;
                }
                catch { trans.Rollback(); throw; }
            }
        }
    }
}