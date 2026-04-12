using System;

namespace HardwareManagementSystem
{
    public class Staff
    {
        public int StaffID { get; set; }
        public string FullName { get; set; }
        public string NIC { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public decimal Salary { get; set; }
        public DateTime StartingDate { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Brand { get; set; }
        public decimal CurrentSellingPrice { get; set; }
        public decimal TotalQuantity { get; set; }
        public string SellingUnit { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        public string ImagePath { get; set; }
        public int StaffID { get; set; }
    }

    public class Batch
    {
        public int BatchID { get; set; }
        public string BatchNumber { get; set; }
        public decimal CostPrice { get; set; }
        public decimal InitialQuantity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public DateTime ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int ProductID { get; set; }
        public int SupplierID { get; set; } = 1;
    }

    public class Debtor
    {
        public int DebtorID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public int StaffID { get; set; }
    }

    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal OriginalUnitPrice { get; set; } // NEW: Tracks the true price
        public decimal UnitPrice { get; set; } // The price after haggling
        public decimal Quantity { get; set; }
        public string SellingUnit { get; set; }
        public decimal TotalPrice { get; set; }
        // Automatically calculates how much money was taken off this specific item
        public decimal LineDiscount => (OriginalUnitPrice - UnitPrice) * Quantity;
    }

    public class Bill
    {
        public int BillID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public decimal SubTotal { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentType { get; set; }
        public string Status { get; set; }
        public int StaffID { get; set; }
        public int? DebtorID { get; set; }
    }
}