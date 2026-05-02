using System;

namespace HardwareManagementSystem.Models 
{
    public class Supplier
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Category { get; set; }
        public Supplier()
        {
            SupplierName = "";
            Category = "General";
        }
    }
}