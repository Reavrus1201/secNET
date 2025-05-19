# secNET

This is an ASP.NET Razor Pages application for managing security logs.

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

## Prerequisites
- **.NET Core 3.0 SDK**: Ensure the .NET Core 3.0 SDK is installed. Download from [here](https://dotnet.microsoft.com/en-us/download/dotnet/3.0).
- **SQL Server**: A SQL Server instance (e.g., LocalDB or a full SQL Server) is required. LocalDB is recommended for development.
- **SQL Server Management Studio (SSMS)**: Optional for database setup. Download from [here](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).

## Setup Instructions
1. **Clone the Repository**: git clone https://github.com/Reavrus1201/secNET.git

2. **Install Dependencies**:
Install NuGet packages by running: dotnet restore
Or install individually:
dotnet add package Isopoh.Cryptography.Argon2 --version 2.0.0
dotnet add package iTextSharp --version 5.5.13.4
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 3.0.0
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.Abstractions --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 3.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 3.0.0
dotnet add package Microsoft.VisualStudio.Azure.Containers.Tools.Targets --version 1.21.0

3. **Set Up Configuration**:
- The project requires a database connection string and a JWT secret key.
- Open `appsettings.json` and `appsettings.Development.json` and update:
  - `ConnectionStrings:DefaultConnection`: Replace with your database connection string (e.g., provided in email).
  - `JwtSettings:SecretKey`: Replace with your JWT secret key (must be at least 32 characters long, provided in email).
- Alternatively, set as environment variables:
  - `ConnectionStrings__DefaultConnection`
  - `JwtSettings__SecretKey`

4. **Create the Database**:
- Open SQL Server Management Studio (SSMS).
- Connect to your SQL Server instance.
- Right-click "Databases" and select "Restore Database".
- Choose the `.bak` file provided in email.
- Follow the prompts to restore the database.

5. **Run the Application**:
Open in Visual Studio and run, or use the command line: dotnet run

## Notes
- Ensure SQL Server LocalDB is installed if using the default connection string.
- The JWT secret key must match the one used in your authentication setup.
- A SQL Server instance (e.g., LocalDB or a full SQL Server) is required.
- If you don’t have the connection string or JWT key, contact the project admin.

## Contributing
- Fork the repository.
- Create a new branch (`git checkout -b feature/your-feature`).
- Commit your changes (`git commit -m 'Add your feature'`).
- Push to the branch (`git push origin feature/your-feature`).
- Open a Pull Request on GitHub.
