using System;

namespace HardwareManagementSystem.Models
{
    // A C# class to represent one single row from the 'Product' database table
    public class ProductModel
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Brand { get; set; }
        public decimal CurrentSellingPrice { get; set; }
        public int TotalQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        public int StaffID { get; set; } // Foreign Key
    }
}