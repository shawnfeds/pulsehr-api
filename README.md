# PulseHR Backend API

PulseHR Backend is a production-ready REST API built using **.NET 10 (C#)** and **Entity Framework Core (EF Core)**. It provides backend capabilities for the PulseHR management portal, replacing local browser mocks with real SQL Server database persistence, secure JWT token-based authentication, structured request logging, and granular rate-limiting.

---

## 🚀 Tech Stack & Architectures
* **Runtime**: .NET 10 Web API
* **Database**: Microsoft SQL Server
* **ORM**: Entity Framework Core 9 (Database-First Scaffolded)
* **Authentication**: JWT Bearer Access Tokens + rotatable `HttpOnly` Secure Refresh Tokens
* **Logging**: Structured Request Logging via **Serilog** with console output and daily rolling log files
* **Rate Limiting**: sliding window rate limits on auth (10/min), file uploads (10/min), and global request endpoints (100/min)
* **Exception Handling**: Global middleware handling database constraints, auth issues, and server errors into standardized camelCase JSON envelopes
* **Documentation**: Interactive OpenAPI UI using Swagger

---

## 📁 Directory Structure
```
PulseHR-API/
├── create_db.sql                  # Database Schema setup DDL
├── seed_db.sql                    # Initial seed user accounts script
├── REMAINING_TASKS.md             # Developer handoff checklist
├── .gitignore                     # Git settings ignoring bin/obj/logs/uploads
├── README.md                      # This documentation file
└── PulseHR.Api/
    ├── Controllers/               # REST Endpoints (Auth, Employees, Projects, etc.)
    ├── Data/                      # Scaffolded DbContext (PulseHRContext.cs)
    ├── Models/                    # Scaffolded Entity Framework models (Employee.cs, Leave.cs, etc.)
    ├── DTOs/                      # Request/Response Data Transfer Objects
    ├── Middleware/                # GlobalExceptionMiddleware
    ├── Services/                  # Business services (Token, Auth, FileStorage)
    ├── Program.cs                 # App setup, middleware pipeline configuration
    ├── appsettings.json           # Configurations (JWT, CORS, RateLimiting, Serilog)
    └── wwwroot/                   # Uploaded static files (avatars, PDF policies)
```

---

## 🛠️ Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [MS SQL Server](https://www.microsoft.com/sql-server/) (Express, Developer, or localdb)
* Command-line shell (PowerShell or Bash)

---

### Step 1: Database Setup
Before scaffolding or running the API, you must create and seed the SQL Server database.

1. **Create the Database and Tables**:
   Open a SQL Server Management Studio (SSMS) query window (or use `sqlcmd`) and execute [create_db.sql](create_db.sql).
   This creates the `PulseHR` database and 15 tables with PKs, FKs, constraints, and persisted computed columns.

2. **Seed Initial Accounts**:
   Run [seed_db.sql](seed_db.sql) to seed test accounts:
   * **Admin User**: `admin@pulsehr.com` (Password: `AdminPassword123!`)
   * **Employee User**: `priya@pulsehr.com` (Password: `EmployeePassword123!`)

---

### Step 2: Build & Run the API
1. Navigate to the API project folder:
   ```bash
   cd PulseHR.Api
   ```
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the development server:
   ```bash
   dotnet run
   ```
   The terminal will output the HTTPS and HTTP URLs (e.g. `https://localhost:7198`).

---

### Step 3: Verify the API
1. **Interactive Swagger Documentation**:
   Navigate to the Swagger UI in your browser:
   `https://localhost:7198/swagger`
   
2. **Ping Health Check**:
   Submit a `GET` request to `https://localhost:7198/health`. A response of `Healthy` indicates successful database connectivity.

---

## 🔒 Security Hardening in Production

To transition this codebase to a live production server, ensure the following steps are configured:

1. **Move Secrets**:
   Move connection strings and JWT signing keys out of `appsettings.json` and configure them as **Environment Variables** on your server (e.g. `ConnectionStrings__DefaultConnection` and `Jwt__Key`).
2. **Upload Folder Permissions**:
   The static file uploads directories (avatars and PDF policy docs) are saved under `PulseHR.Api/wwwroot/uploads/`. Ensure that the service account executing your application process has **Write Permissions** on this folder.
3. **Secure Cookies**:
   The backend writes refresh tokens using a secure `HttpOnly` cookie. Secure cookies require HTTPS to be delivered. Make sure your server has a valid SSL certificate.

---

## 💻 Connecting Javascript Frontend
1. Open the frontend repository's API configuration file (commonly `js/services/api.js`).
2. Locate the mock mode configurations and set mock mode to false:
   ```javascript
   window.MOCK_MODE = false;
   ```
3. Update the backend API base url configuration variable to match your running server's url:
   ```javascript
   const BASE_URL = "https://localhost:7198/api";
   ```
4. Confirm that your AJAX or fetch library incorporates cookie credentials in requests so the secure `HttpOnly` cookie is passed back:
   ```javascript
   credentials: 'include'
   ```
