# PulseHR API — Remaining Tasks & Handover Document

This document lists the completed work, verification steps, and remaining tasks for the PulseHR production-ready .NET 10 API.

---

## 🛠️ What has been completed
1. **Database Schema Creation & Execution**: Created the full MS SQL Server script (`create_db.sql`) defining 15 tables with proper indexes, constraints, relationship cascades, and a persisted computed column (`LeaveBalances.Balance` = `Total - Used`).
2. **Project Setup**: Bootstrap of the .NET 10 API project (`PulseHR.Api`) with dependencies:
   - `Microsoft.EntityFrameworkCore.SqlServer`
   - `Microsoft.EntityFrameworkCore.Design`
   - `Microsoft.AspNetCore.Authentication.JwtBearer`
   - `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
   - `BCrypt.Net-Next`
   - `CsvHelper`
   - `Swashbuckle.AspNetCore`
   - `Serilog.AspNetCore` & `Serilog.Sinks.File`
3. **Database-First Scaffolding**: Successfully scaffolded models and context directly from your SQL Server database. The classes (including `Leave.cs`, `Employee.cs`, etc.) and `PulseHRContext` are fully generated in the `Models` and `Data` folders.
4. **Git Configuration**: Added a comprehensive `.gitignore` file to ensure build outputs (`bin/`, `obj/`, `.vs/`) and local uploads directory are ignored.
5. **JWT & Refresh Token Flow**:
   - Access tokens (15-min lifetime) with claims including EmployeeId (`sub`), email, name, `isAdmin`, and `userType`.
   - Refresh tokens (7-day lifetime) with automatic rotation (new refresh token issued on every refresh) and family-wide reuse detection (compromised tokens revoke all descendant tokens).
   - Login, refresh, logout, /me, and password change endpoints in `AuthController`.
6. **Production Middleware & Logging**:
   - Structured Logging: Serilog integration logging to console and daily rolling text files in `Logs/` directory.
   - Global exception middleware mapping errors to standard JSON response envelopes.
   - Built-in rate limiting policies (`AuthLimit`, `UploadLimit`, and IP/user-keyed `GlobalLimit`) returning 429 and `Retry-After` headers.
   - Configurable CORS.
   - Swagger / OpenAPI UI with JWT Bearer authentication metadata.
   - Response compression & Health checks endpoint.
7. **Feature Domain Controllers**:
   - `EmployeesController`: full CRUD, roles/projects assignments, histories, and file-based avatar upload.
   - `ProjectsController`: CRUD, search, and member management.
   - `LeavesController`: CRUD, leave requests, leave balances, and admin approval which automatically adjusts `Used` balances on the `LeaveBalances` table.
   - `TimesheetsController`: CRUD, month filtering, and memory-buffered CSV exports.
   - `PoliciesController`: categories metadata, uploads (saving on local disk) and binary stream downloads.
   - `HolidaysController` & `EventsController`: full CRUD.
   - `DashboardController`: aggregated stats for admin / per-employee attendance rate math.
   - `SearchController`: global unified search with partial matching.
   - `NotificationsController`: alerts lists and read states.

---

## 🚦 Verification & How to Run

1. **Start the API**:
   ```bash
   cd d:\Projects\PulseHR-API\PulseHR.Api
   dotnet run
   ```
2. **Open Swagger UI**:
   Navigate to `https://localhost:7198/swagger/index.html` (or whatever HTTPS port the terminal prints) in your browser.
3. **Check health status**:
   - Hit `GET /health` (no auth required) to verify database connectivity.

---

## 📝 Remaining Tasks

Here is what is left to do next:

### 1. Verification & Testing
- [/] **Swagger Testing**: Authorize in Swagger and perform a sanity run through the auth endpoints (employee login, get profile, request leaves, etc.).
- [ ] **DB Seed Data validation**: Ensure the database has at least one Admin and one Employee account with valid BCrypt password hashes so you can login.
  - *Note: To generate a BCrypt hash for testing, you can use online BCrypt generators or a temporary console tool, using the password you want (e.g. `admin123`).*

### 2. Frontend Integration
- [ ] **Disable Mock Mode**: In `js/services/api.js`, set `window.MOCK_MODE = false;` and set the `BASE_URL` to point to the .NET API (e.g. `https://localhost:7198/api`).
- [ ] **Refresh Token handling**:
  - The API supports refresh tokens in the JSON response payload (`refreshToken`) as well as reading cookies.
  - Update `api.js` to store the refresh token and automatically invoke `/api/auth/refresh` on a `401 Unauthorized` interceptor.

### 3. File upload directory setup
- The file upload base path is configured in `appsettings.json` under `FileStorage:BasePath` as `wwwroot/uploads`.
- Make sure that folder write permissions are enabled if deploying to a production server.
