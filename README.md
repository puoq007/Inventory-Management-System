# 📦 Inventory System

A modern, full-stack web application designed for comprehensive tracking and management of manufacturing jigs and associated data. Built with Blazor Web App, Entity Framework Core, and Tailwind CSS.

## 🌟 Key Features

* **Role-Based Access Control (RBAC):** Secure access levels (Admin, Engineer, ProdLead, Operator, Guest) controlling who can view or manage data.
* **Master Data Management:** 
  * `Jig Specs`: Base blueprints and specifications of jigs.
  * `Locations`: Physical storage locations (Site/Cabinet/Shelf/Position).
  * `Part Mappings (BOMs)`: Connect specific parts to required jig specifications.
* **Physical Asset Tracking:** Track individual physical jigs (`Physical Jigs`), their status (Available, In Use), condition, and precise location.
* **Excel & CSV Import:** Powerful bulk data upload capabilities allowing you to populate data directly from `.xlsx` or `.csv` files.
* **QR Code Generation:** Instantly generate and print QR codes for physical jigs for quick scanning on the factory floor.
* **Modern UI:** Responsive, aesthetically pleasing interface powered by Tailwind CSS.

## 🏗️ Project Structure

The project follows a standard Blazor Server architecture:

```text
📂 Inventory/
├── 📂 Components/
│   ├── 📂 Layout/        # Main application layout, sidebar, and navigation components.
│   ├── 📂 Pages/         # Blazor pages (UI views).
│   │   ├── 📂 Admin/     # Management pages (JigSpecs, PhysicalJigs, Locators, etc.).
│   │   └── ...           # Public/User pages (Dashboard, Login, History).
│   ├── 📂 Shared/        # Reusable UI components (e.g., RoleGate for authorization).
│   ├── App.razor         # Root component.
│   └── Routes.razor      # Application routing configuration.
├── 📂 Models/            # C# entity classes defining the database structure (JigSpec, PhysicalJig, etc.).
├── 📂 Services/          # Business logic and external service integrations.
│   ├── AuthService.cs    # Handles user authentication and role management.
│   ├── ExcelImportService.cs # Handles parsing and importing Excel/CSV data.
│   └── SeedDataService.cs # Initial database population logic.
├── 📂 Data/              # Entity Framework Core context (`AppDbContext`).
├── 📂 wwwroot/           # Static web assets (CSS, images, JS).
├── appsettings.json      # Configuration file (Database connection strings).
└── Program.cs            # Application entry point and service registration.
```

## 🚀 Technology Stack

* **Frontend & Backend Framework:** [Blazor Web App (.NET 8/9)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
* **Styling:** [Tailwind CSS](https://tailwindcss.com/)
* **Database Access:** [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
* **Database Engine:** Microsoft SQL Server
* **Data Processing:** ExcelDataReader (for Excel/CSV imports)
* **QR Generation:** Net.Codecrete.QrCodeGenerator

## 💡 Getting Started

1. **Prerequisites:** Ensure you have the .NET SDK installed and access to a SQL Server database.
2. **Database Configuration:** Update the `DefaultConnection` string in `appsettings.json` to point to your SQL Server instance.
3. **Run Migrations/Create DB:** The application is configured to automatically ensure the database is created upon starting.
4. **Launch:** Run the application using `dotnet run --urls "http://localhost:5101"`.
5. **Initial Login:** The system is seeded with a default admin user `admin` and password `admin`.
# Inventory-Management-System
