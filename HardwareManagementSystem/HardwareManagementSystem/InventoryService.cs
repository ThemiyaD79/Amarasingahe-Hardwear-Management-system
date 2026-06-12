using System;
using System.Data;
using System.Data.SQLite;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class InventoryService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public void ProcessProductInventory(Product prod, Batch batch)
        {
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                con.Open();
                SQLiteTransaction trans = con.BeginTransaction();
                try
                {
                    if (prod.ProductID == 0)
                    {
                        string pSql = @"INSERT INTO Product (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID, ImagePath) 
                                        VALUES (@n, @c, @b, @price, @q, @r, @a, @sid, @img); SELECT last_insert_rowid();";
                        SQLiteCommand cmd = new SQLiteCommand(pSql, con, trans);
                        cmd.Parameters.AddWithValue("@n", prod.Name);
                        cmd.Parameters.AddWithValue("@c", prod.Category);
                        cmd.Parameters.AddWithValue("@b", prod.Brand);
                        cmd.Parameters.AddWithValue("@price", prod.CurrentSellingPrice);
                        cmd.Parameters.AddWithValue("@q", batch.InitialQuantity);
                        cmd.Parameters.AddWithValue("@r", prod.ReorderLevel);
                        cmd.Parameters.AddWithValue("@a", prod.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@sid", prod.StaffID);
                        cmd.Parameters.AddWithValue("@img", prod.ImagePath ?? (object)DBNull.Value);

                        prod.ProductID = Convert.ToInt32(cmd.ExecuteScalar());

                        string bSql = @"INSERT INTO Batch (BatchNumber, CostPrice, InitialQuantity, CurrentQuantity, ManufacturingDate, ProductID, SupplierID) 
                                        VALUES (@bn, @cp, @iq, @iq, CURRENT_TIMESTAMP, @pid, 1)";
                        SQLiteCommand bCmd = new SQLiteCommand(bSql, con, trans);
                        bCmd.Parameters.AddWithValue("@bn", batch.BatchNumber);
                        bCmd.Parameters.AddWithValue("@cp", batch.CostPrice);
                        bCmd.Parameters.AddWithValue("@iq", batch.InitialQuantity);
                        bCmd.Parameters.AddWithValue("@pid", prod.ProductID);
                        bCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        string uSql = @"UPDATE Product SET Name=@n, Category=@c, Brand=@b, CurrentSellingPrice=@price, 
                                        ReorderLevel=@r, IsActive=@a, ImagePath=@img WHERE ProductID=@id";
                        SQLiteCommand uCmd = new SQLiteCommand(uSql, con, trans);
                        uCmd.Parameters.AddWithValue("@id", prod.ProductID);
                        uCmd.Parameters.AddWithValue("@n", prod.Name);
                        uCmd.Parameters.AddWithValue("@c", prod.Category);
                        uCmd.Parameters.AddWithValue("@b", prod.Brand);
                        uCmd.Parameters.AddWithValue("@price", prod.CurrentSellingPrice);
                        uCmd.Parameters.AddWithValue("@r", prod.ReorderLevel);
                        uCmd.Parameters.AddWithValue("@a", prod.IsActive ? 1 : 0);
                        uCmd.Parameters.AddWithValue("@img", prod.ImagePath ?? (object)DBNull.Value);
                        uCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
                catch { trans.Rollback(); throw; }
            }
        }

        public DataTable GetStockBySupplier(int supplierId)
        {
            string query = @"
                SELECT 
                    p.Name AS ProductName, 
                    SUM(b.CurrentQuantity) AS TotalStockLeft,
                    p.SellingUnit
                FROM Batch b
                JOIN Product p ON b.ProductID = p.ProductID
                WHERE b.SupplierID = @SupID AND b.CurrentQuantity > 0
                GROUP BY p.Name, p.SellingUnit";

            DataTable dt = new DataTable();
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@SupID", supplierId);
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public DataTable GetLowStockItems()
        {
            string query = @"
                SELECT ProductID, Name, TotalQuantity 
                FROM Product 
                WHERE TotalQuantity <= ReorderLevel AND IsActive = 1 
                ORDER BY TotalQuantity ASC";

            DataTable dt = new DataTable();
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
    }
}