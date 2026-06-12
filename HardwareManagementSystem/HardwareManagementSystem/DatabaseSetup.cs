using System;
using System.IO;
using System.Data.SQLite;
using System.Windows;

namespace HardwareManagementSystem
{
    public static class DatabaseSetup
    {
        // The database file will be created in the exact same folder as your .exe
        private static string dbPath = "HardwareDB.db";
        private static string connectionString = $"Data Source={dbPath};Version=3;";

        public static void InitializeOnStartup()
        {
            // 1. Check if the database file already exists. If it does, do nothing!
            if (!File.Exists(dbPath))
            {
                MessageBox.Show("First-time setup detected. Creating the portable database...", "System Setup", MessageBoxButton.OK, MessageBoxImage.Information);

                try
                {
                    // 2. Create the blank physical .db file
                    SQLiteConnection.CreateFile(dbPath);

                    // 3. Connect to the new file and build all the tables and seed data
                    using (SQLiteConnection con = new SQLiteConnection(connectionString))
                    {
                        con.Open();
                        string schemaScript = GetDatabaseSchemaScript();

                        using (SQLiteCommand cmd = new SQLiteCommand(schemaScript, con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Database created successfully! You can now log in.", "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database setup failed.\n\nError: " + ex.Message, "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static string GetDatabaseSchemaScript()
        {
            // This is the exact SQLite translation of your master script
            return @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE Staff ( StaffID INTEGER PRIMARY KEY AUTOINCREMENT, FullName TEXT NOT NULL, NIC TEXT, Phone TEXT, Username TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL, Role TEXT NOT NULL, Salary NUMERIC DEFAULT 0, StartingDate DATETIME DEFAULT CURRENT_TIMESTAMP, Age INTEGER, IsActive INTEGER DEFAULT 1 );
                
                CREATE TABLE Supplier ( SupplierID INTEGER PRIMARY KEY AUTOINCREMENT, SupplierName TEXT NOT NULL, CompanyName TEXT, Phone TEXT, Address TEXT, Category TEXT, OutstandingBalance NUMERIC DEFAULT 0.00 );
                
                CREATE TABLE Debtor ( DebtorID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Phone TEXT NOT NULL UNIQUE, Address TEXT, TotalOutstandingAmount NUMERIC DEFAULT 0.00, StaffID INTEGER, FOREIGN KEY(StaffID) REFERENCES Staff(StaffID) );
                
                CREATE TABLE Product ( ProductID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Category TEXT, SubCategory TEXT, Brand TEXT, CurrentSellingPrice NUMERIC NOT NULL, TotalQuantity NUMERIC DEFAULT 0.00, SellingUnit TEXT, ReorderLevel INTEGER DEFAULT 0, IsActive INTEGER DEFAULT 1, ImagePath TEXT, StaffID INTEGER, FOREIGN KEY(StaffID) REFERENCES Staff(StaffID) );
                
                CREATE TABLE Batch ( BatchID INTEGER PRIMARY KEY AUTOINCREMENT, BatchNumber TEXT, CostPrice NUMERIC NOT NULL, InitialQuantity NUMERIC NOT NULL, CurrentQuantity NUMERIC NOT NULL, ManufacturingDate DATETIME DEFAULT CURRENT_TIMESTAMP, ProductID INTEGER, SupplierID INTEGER, FOREIGN KEY(ProductID) REFERENCES Product(ProductID), FOREIGN KEY(SupplierID) REFERENCES Supplier(SupplierID) );
                
                CREATE TABLE Bill ( BillID INTEGER PRIMARY KEY AUTOINCREMENT, Date TEXT NOT NULL, Time TEXT NOT NULL, SubTotal NUMERIC NOT NULL, BillDiscount NUMERIC DEFAULT 0.00, GrandTotal NUMERIC NOT NULL, PaymentType TEXT NOT NULL, Status TEXT DEFAULT 'Completed', StaffID INTEGER, DebtorID INTEGER, FOREIGN KEY(StaffID) REFERENCES Staff(StaffID), FOREIGN KEY(DebtorID) REFERENCES Debtor(DebtorID) );
                
                INSERT INTO sqlite_sequence (name, seq) VALUES ('Bill', 999);
                
                CREATE TABLE Bill_Item ( BillItemID INTEGER PRIMARY KEY AUTOINCREMENT, QuantitySold NUMERIC NOT NULL, LineDiscount NUMERIC DEFAULT 0.00, SellingPriceAtTimeOfSale NUMERIC NOT NULL, BillID INTEGER, ProductID INTEGER, BatchID INTEGER, FOREIGN KEY(BillID) REFERENCES Bill(BillID), FOREIGN KEY(ProductID) REFERENCES Product(ProductID), FOREIGN KEY(BatchID) REFERENCES Batch(BatchID) );
                
                CREATE TABLE Debt_Transaction ( TransactionID INTEGER PRIMARY KEY AUTOINCREMENT, Date DATETIME DEFAULT CURRENT_TIMESTAMP, Type TEXT NOT NULL, Amount NUMERIC NOT NULL, DebtorID INTEGER, StaffID INTEGER, FOREIGN KEY(DebtorID) REFERENCES Debtor(DebtorID), FOREIGN KEY(StaffID) REFERENCES Staff(StaffID) );
                
                CREATE TABLE AuditLog ( LogID INTEGER PRIMARY KEY AUTOINCREMENT, Date DATETIME DEFAULT CURRENT_TIMESTAMP, Action TEXT NOT NULL, Description TEXT, StaffID INTEGER, FOREIGN KEY(StaffID) REFERENCES Staff(StaffID) );

                INSERT INTO Staff (FullName, Username, PasswordHash, Role, IsActive) VALUES ('System Administrator', 'manager1', 'admin123', 'Manager', 1);
                INSERT INTO Staff (FullName, Username, PasswordHash, Role, IsActive) VALUES ('General Cashier', 'cashier1', 'cash123', 'Cashier', 1);
                INSERT INTO Supplier (SupplierName, CompanyName, Category) VALUES ('Default Local Supplier', 'Various', 'General');
            ";
        }
    }
}