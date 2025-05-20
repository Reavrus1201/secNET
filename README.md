# secNET

This is an ASP.NET Razor Pages application for managing security logs, designed for Woermann Brock & Co. Pty. Ltd.

## Features
- **CCTV Log Management**: Log and review CCTV footage details.
- **Incident Reporting**: Submit and track security incidents.
- **User Tiers**: Role-based access for different user levels (Tier 1, Tier 2, Tier 3).
- **Branch Selection**: Assign users to specific branches for localized management.
- **User Management**: Admins can create, update, and delete user accounts.
- **Incident Management**: Admins can create, update, and delete incidents.
- **CCTV Management**: Admins can create, update, and delete CCTV records.
- **Audit Logs**: Track changes made to incidents, users, and CCTV records.
- **Search and Filter**: Easily search and filter incidents, users, and CCTV records.
- **HTML Report Downloads**: Export CCTV logs and incident reports as HTML files for printing or sharing.

## Prerequisites
- **.NET Core 3.0 SDK**: Ensure the .NET Core 3.0 SDK is installed. Download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/3.0) if needed.
- **SQL Server**: A SQL Server instance (e.g., LocalDB or a full SQL Server) is required. LocalDB is recommended for development and can be installed with Visual Studio or the SQL Server Express installer.
- **SQL Server Management Studio (SSMS)**: Optional but recommended for database setup. Download from [here](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
- **Visual Studio 2022**: Recommended for development (or another IDE that supports ASP.NET Core).

## Setup Instructions

1. **Clone the Repository**:
   Clone the project from GitHub using the following command:
git clone https://github.com/yourusername/secNET.git
cd secNET

2. **Install Dependencies**:
Install the required NuGet packages by running: "dotnet restore"
Alternatively, you can install each package individually:
dotnet add package Isopoh.Cryptography.Argon2 --version 2.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 3.0.0
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.Abstractions --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 3.0.0
dotnet add package Microsoft.VisualStudio.Azure.Containers.Tools.Targets --version 1.21.0

3. **Set Up Configuration**:
- The project requires a database connection string and a JWT secret key.
- Open `appsettings.json` and `appsettings.Development.json` and update the following values:
  - `ConnectionStrings:DefaultConnection`: Replace with your database connection string (provided via email by your administrator).
  - `JwtSettings:SecretKey`: Replace with your JWT secret key (must be at least 32 characters long, provided via email by your administrator).
- If you don’t have these values, contact your project administrator for the email containing the credentials.

4. **Create the Database**:
- Open SQL Server Management Studio (SSMS).
- Connect to your SQL Server instance (e.g., `(localdb)\MSSQLLocalDB` or `DESKTOP-9HBIT6M\SQLEXPRESS` if previously set up).
- Right-click on the "Databases" node and select "Restore Database".
- Choose the `.bak` file (provided via email by your administrator).
- Follow the prompts to restore the database, ensuring the database name matches `secNET`.
- Note: Ensure your SQL Server version is compatible with the backup file (SQL Server 2019 or later recommended).

5. **Run the Application**:
Open the solution in Visual Studio 2022 and press `F5` to run, or use the command line: "dotnet run"