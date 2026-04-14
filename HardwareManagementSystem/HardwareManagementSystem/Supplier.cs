using System;

namespace HardwareManagementSystem.Models // Adjust namespace to match your project
{
    public class Supplier
    {
        // Encapsulated properties mapping to your database columns
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Category { get; set; }

        // Optional: A constructor to initialize a blank supplier
        public Supplier()
        {
            SupplierName = "";
            Category = "General";
        }
    }
}