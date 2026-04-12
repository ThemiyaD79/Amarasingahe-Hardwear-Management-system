using System;
using System.Data.SqlClient;
using System.Configuration;

namespace HardwareManagementSystem
{
    public class InventoryService
    {
        private string connString = ConfigurationManager.ConnectionStrings["HardwareDBConnection"].ConnectionString;

        public void ProcessProductInventory(Product prod, Batch batch)
        {
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    if (prod.ProductID == 0)
                    {
                        string pSql = @"INSERT INTO Product (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID, ImagePath) 
                                        VALUES (@n, @c, @b, @price, @q, @r, @a, @sid, @img); SELECT SCOPE_IDENTITY();";
                        SqlCommand cmd = new SqlCommand(pSql, con, trans);
                        cmd.Parameters.AddWithValue("@n", prod.Name);
                        cmd.Parameters.AddWithValue("@c", prod.Category);
                        cmd.Parameters.AddWithValue("@b", prod.Brand);
                        cmd.Parameters.AddWithValue("@price", prod.CurrentSellingPrice);
                        cmd.Parameters.AddWithValue("@q", batch.InitialQuantity);
                        cmd.Parameters.AddWithValue("@r", prod.ReorderLevel);
                        cmd.Parameters.AddWithValue("@a", prod.IsActive);
                        cmd.Parameters.AddWithValue("@sid", prod.StaffID);
                        cmd.Parameters.AddWithValue("@img", prod.ImagePath ?? (object)DBNull.Value);

                        prod.ProductID = Convert.ToInt32(cmd.ExecuteScalar());

                        string bSql = @"INSERT INTO Batch (BatchNumber, CostPrice, InitialQuantity, CurrentQuantity, ManufacturingDate, ProductID, SupplierID) 
                                        VALUES (@bn, @cp, @iq, @iq, GETDATE(), @pid, 1)";
                        SqlCommand bCmd = new SqlCommand(bSql, con, trans);
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
                        SqlCommand uCmd = new SqlCommand(uSql, con, trans);
                        uCmd.Parameters.AddWithValue("@id", prod.ProductID);
                        uCmd.Parameters.AddWithValue("@n", prod.Name);
                        uCmd.Parameters.AddWithValue("@c", prod.Category);
                        uCmd.Parameters.AddWithValue("@b", prod.Brand);
                        uCmd.Parameters.AddWithValue("@price", prod.CurrentSellingPrice);
                        uCmd.Parameters.AddWithValue("@r", prod.ReorderLevel);
                        uCmd.Parameters.AddWithValue("@a", prod.IsActive);
                        uCmd.Parameters.AddWithValue("@img", prod.ImagePath ?? (object)DBNull.Value);
                        uCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
                catch { trans.Rollback(); throw; }
            }
        }
    }
}