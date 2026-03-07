using System;

namespace HardwareManagementSystem.Models
{
    public class StaffModel
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
}