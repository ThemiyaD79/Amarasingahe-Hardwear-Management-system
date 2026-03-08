using System;
using System.Data.SqlClient;
using HardwareManagementSystem.Database;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem.Repositories
{
    public class ProductRepository
    {
        // Add new product
        public void AddProduct(ProductModel product)
        {
            using (SqlConnection conn = DbConnection.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO Product
                (Name, Category, Brand, CurrentSellingPrice, TotalQuantity, ReorderLevel, IsActive, StaffID)
                VALUES
                (@Name,@Category,@Brand,@Price,@Quantity,@Reorder,1,1)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", product.Name);
                cmd.Parameters.AddWithValue("@Category", product.Category);
                cmd.Parameters.AddWithValue("@Brand", product.Brand);
                cmd.Parameters.AddWithValue("@Price", product.CurrentSellingPrice);
                cmd.Parameters.AddWithValue("@Quantity", product.TotalQuantity);
                cmd.Parameters.AddWithValue("@Reorder", product.ReorderLevel);

                cmd.ExecuteNonQuery();
            }
        }

        // Update existing product
        public void UpdateProduct(ProductModel product)
        {
            using (SqlConnection conn = DbConnection.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE Product SET
                                 Name=@Name,
                                 Category=@Category,
                                 Brand=@Brand,
                                 CurrentSellingPrice=@Price,
                                 TotalQuantity=@Quantity,
                                 ReorderLevel=@Reorder
                                 WHERE ProductID=@ProductID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", product.Name);
                cmd.Parameters.AddWithValue("@Category", product.Category);
                cmd.Parameters.AddWithValue("@Brand", product.Brand);
                cmd.Parameters.AddWithValue("@Price", product.CurrentSellingPrice);
                cmd.Parameters.AddWithValue("@Quantity", product.TotalQuantity);
                cmd.Parameters.AddWithValue("@Reorder", product.ReorderLevel);
                cmd.Parameters.AddWithValue("@ProductID", product.ProductID);

                cmd.ExecuteNonQuery();
            }
        }

        // Delete product
        public void DeleteProduct(int productId)
        {
            using (SqlConnection conn = DbConnection.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM Product WHERE ProductID=@ProductID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.ExecuteNonQuery();
            }
        }



    }

}