🚀 Team Quick-Start Guide: Cloning & Coding
Welcome to the Amarasinghe Hardware POS project! 🎉 The foundational architecture and database connection are officially live. Please follow these exact steps to get set up on your local machines and start coding.
Provide All the GitHub usernames to owner of the repository that way he can add you as collaborator then only you can processed with the following step. Once he adds you to the repository you may have to accept the invitation. 
📥 Phase 1: Get the Code (Cloning)
You do not need GitHub Desktop. We will do everything right inside Visual Studio 2022.
•	Step 1: Open Visual Studio 2022 and click "Clone a repository".
•	Step 2: Paste our project link: https://github.com/ThemiyaD79/Amarasingahe-Hardwear-Management-system.git 
•	Step 3: Choose a safe, empty folder on your computer and click Clone.
•	Step 4: Visual Studio will download the entire project. In the Solution Explorer (on the right side), you will see all the folders (Database, Forms, etc.) already set up!
🗄️ Phase 2: Connect Your Local Database (Crucial Step!)
The code currently points to Themiya's SQL Server. You must point it to your own SQL Server to run the app.
•	Step 1: Open SQL Server Management Studio (SSMS) on your computer. Create a new database named AmarasingheHardwareDB, and run the master SQL script provided by Diluni to build the tables.
•	Step 2: In Visual Studio, open the App.config file.
•	Step 3: Look for this line within the connection string: Data Source=themiyaft;
•	Step 4: Change themiyaft to your own personal SQL Server name.
•	⚠️ MASSIVE WARNING: When you save your code later, DO NOT commit the App.config file! If you upload your modified App.config, it will overwrite the server name on GitHub and break the app for everyone else. Always right-click App.config in your Git Changes window and select Undo Changes before you commit your actual C# code.
🌿 Phase 3: Start Coding (The Branching Rule)
To prevent code conflicts and keep our main branch clean, nobody is allowed to code directly on the main branch.
•	Step 1: Look at the bottom right corner of Visual Studio. You will see the word main with a small pencil icon. Click it.
•	Step 2: Click New Branch.
•	Step 3: Name your branch based on your specific task (e.g., kavisha-login-ui or aloka-inventory-logic).
•	Step 4: Click Create. You are now safe to write code, design forms, and make changes! 💻
🚀 Phase 4: Saving and Uploading (Commit & Push)
When you finish your feature and want to send it to the team:
•	Step 1: Open the Git Changes tab on the right side of Visual Studio.
•	Step 2: Double-check that App.config is NOT in the list of changes. (If it is, undo the change!)
•	Step 3: Type a short, clear message explaining what you did (e.g., "Finished the UI for the Cashier Dashboard").
•	Step 4: Click Commit All.
•	Step 5: Click the Up Arrow (Push) at the top of the Git Changes window to send it to GitHub.
•	Step 6: Message Themiya (Project Manager) to let him know your branch is live so he can review and merge it! 🤝
________________________________________
