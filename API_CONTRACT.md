# NexusHR — API Contract
# .NET Integration Reference
# All endpoints expect/return JSON. Base URL: https://your-api.com/api
# Authentication: Bearer JWT in Authorization header (except auth endpoints)
# Standard success envelope: { "success": true, "data": <payload> }
# Standard error envelope:   { "success": false, "message": "description" }

---

## AUTH

### POST /auth/employee/login
Request:
  { "email": "string", "password": "string" }
Response:
  {
    "id": 1, "name": "Priya Sharma", "email": "priya@nexus.io",
    "role": "Software Engineer", "dept": "Engineering",
    "avatar": "PS", "avatarColor": "#7c3aed",
    "phone": "+91 98765 43210", "location": "Bengaluru, KA",
    "joinDate": "2022-03-15", "projects": [1, 3],
    "token": "eyJhbGci...", "expiresAt": "2024-12-31T23:59:59Z"
  }

### POST /auth/admin/login
Request:
  { "email": "string", "password": "string" }
Response: (same shape as employee login, role indicates admin tier)

### POST /auth/logout
Request: (empty body, uses Bearer token)
Response: { "success": true }

### POST /auth/refresh
Request: (empty body, uses Bearer token)
Response: { "token": "new-jwt", "expiresAt": "..." }

### GET /auth/me
Response: (same shape as login response, without token fields)

### POST /auth/change-password
Request:
  { "currentPassword": "string", "newPassword": "string" }
Response: { "success": true }

---

## EMPLOYEES

### GET /employees
Query params: search (string), status (active|inactive), dept (string), page (int), pageSize (int)
Response:
  {
    "data": [
      {
        "id": 1, "name": "Priya Sharma", "email": "priya@nexus.io",
        "role": "Software Engineer", "dept": "Engineering",
        "status": "active", "joinDate": "2022-03-15",
        "projects": [1, 3], "salary": 95000,
        "avatar": "PS", "avatarColor": "#7c3aed",
        "phone": "+91 98765 43210", "location": "Bengaluru, KA"
      }
    ],
    "total": 8
  }

### GET /employees/:id
Response: (single employee object as above)

### GET /employees/:id/profile
Response: (single employee object — same shape)

### POST /employees
Request:
  {
    "name": "string", "email": "string", "role": "string",
    "dept": "string", "joinDate": "YYYY-MM-DD",
    "salary": 90000, "status": "active",
    "phone": "string", "location": "string",
    "avatar": "PS", "avatarColor": "#7c3aed"
  }
Response: (created employee object with id)

### PUT /employees/:id
Request: (same fields as POST, partial update allowed)
Response: (updated employee object)

### PUT /employees/:id/profile
Request:
  { "name": "string", "phone": "string", "location": "string", "email": "string" }
Response: (updated employee object)

### DELETE /employees/:id
Response: { "success": true }

### POST /employees/:id/roles
Request: { "roles": ["Employee", "TeamLead"] }
Response: (updated employee object)

### POST /employees/:id/projects
Request: { "projectIds": [1, 2, 3] }
Response: (updated employee object)

### GET /employees/:id/salary-history
Response:
  {
    "data": [
      { "id": 1, "date": "2022-03-15", "amount": 70000, "note": "Joining CTC" }
    ]
  }

### GET /employees/:id/employment-history
Response:
  {
    "data": [
      { "id": 1, "date": "2022-03-15", "event": "Joined", "role": "Junior SE", "dept": "Engineering" }
    ]
  }

### POST /employees/:id/avatar
Request: multipart/form-data  file: <image file>
Response: { "avatarUrl": "https://cdn.example.com/avatars/1.jpg" }

---

## PROJECTS

### GET /projects
Query params: status (active|on-hold|completed), search (string), page, pageSize
Response:
  {
    "data": [
      {
        "id": 1, "name": "Nexus Platform v2.0",
        "manager": "Rohan Mehta", "status": "active",
        "startDate": "2024-01-15", "endDate": "2024-12-31",
        "progress": 65, "members": [1, 2, 5],
        "description": "Major platform rewrite"
      }
    ],
    "total": 4
  }

### GET /projects/:id
Response: (single project object)

### GET /projects/employee/:employeeId
Response: { "data": [...projects assigned to employee] }

### POST /projects
Request:
  {
    "name": "string", "description": "string",
    "manager": "string", "status": "active",
    "startDate": "YYYY-MM-DD", "endDate": "YYYY-MM-DD",
    "progress": 0
  }
Response: (created project with id, members: [])

### PUT /projects/:id
Request: (same fields as POST)
Response: (updated project object)

### DELETE /projects/:id
Response: { "success": true }

### POST /projects/:id/members
Request: { "employeeIds": [1, 2, 3] }
Response: (updated project object)

### DELETE /projects/:id/members
Request: { "employeeIds": [1] }
Response: (updated project object)

---

## LEAVES

### GET /leaves
Query params: status (pending|approved|rejected), type (Sick|Casual), page, pageSize
Response:
  {
    "data": [
      {
        "id": 1, "employeeId": 1, "employeeName": "Priya Sharma",
        "type": "Sick", "startDate": "2024-04-10", "endDate": "2024-04-11",
        "days": 2, "status": "approved", "reason": "Fever and cold",
        "halfDay": false
      }
    ],
    "total": 5
  }

### GET /leaves/employee/:employeeId
Query params: status (string)
Response: { "data": [...leave objects for that employee] }

### GET /leaves/employee/:employeeId/balance
Response:
  {
    "sick":   { "total": 12, "used": 2,   "balance": 10   },
    "casual": { "total": 12, "used": 0.5, "balance": 11.5 }
  }

### GET /leaves/:id
Response: (single leave object)

### POST /leaves
Request:
  {
    "employeeId": 1, "employeeName": "Priya Sharma",
    "type": "Sick", "startDate": "2024-05-01", "endDate": "2024-05-02",
    "days": 2, "reason": "string", "halfDay": false
  }
Response: (created leave with id, status: "pending")

### PATCH /leaves/:id/status
Request: { "status": "approved" | "rejected", "remarks": "optional string" }
Response: (updated leave object)

### DELETE /leaves/:id
Response: { "success": true }

---

## TIMESHEETS

### GET /timesheets
Query params: page, pageSize
Response: { "data": [...timesheet objects] }

### GET /timesheets/employee/:employeeId
Query params: month (e.g. "April 2024"), page, pageSize
Response:
  {
    "data": [
      {
        "id": 1, "employeeId": 1,
        "date": "2024-04-29", "projectId": 1,
        "project": "Nexus Platform v2.0",
        "task": "Implement auth module",
        "hours": 6, "month": "April 2024"
      }
    ],
    "total": 5
  }

### GET /timesheets/employee/:employeeId/export
Query params: month (string)
Response: text/csv file stream

### GET /timesheets/:id
Response: (single timesheet object)

### POST /timesheets
Request:
  {
    "employeeId": 1, "date": "YYYY-MM-DD",
    "projectId": 1, "project": "string",
    "task": "string", "hours": 6,
    "month": "April 2024"
  }
Response: (created timesheet with id)

### PUT /timesheets/:id
Request: (same fields as POST)
Response: (updated timesheet object)

### DELETE /timesheets/:id
Response: { "success": true }

---

## POLICIES

### GET /policies
Query params: category (HR|IT|Finance|Compliance)
Response:
  {
    "data": [
      {
        "id": 1, "name": "Leave Policy 2024",
        "category": "HR", "uploadedBy": "Sneha Reddy",
        "uploadedOn": "2024-01-10", "size": "245 KB", "type": "pdf"
      }
    ]
  }

### GET /policies/:id
Response: (single policy object)

### POST /policies/upload
Request: multipart/form-data
  name: "string"
  category: "HR" | "IT" | "Finance" | "Compliance"
  file: <pdf/doc file>
Response: (created policy with id)

### DELETE /policies/:id
Response: { "success": true }

### GET /policies/:id/download
Response: file stream (attachment)

---

## HOLIDAYS

### GET /holidays
Query params: year (int, optional)
Response:
  {
    "data": [
      { "id": 1, "name": "Republic Day", "date": "2024-01-26", "type": "National" }
    ]
  }

### POST /holidays
Request: { "name": "string", "date": "YYYY-MM-DD", "type": "National|Festival|Optional" }
Response: (created holiday with id)

### PUT /holidays/:id
Request: (same fields as POST)
Response: (updated holiday)

### DELETE /holidays/:id
Response: { "success": true }

---

## EVENTS

### GET /events
Response:
  {
    "data": [
      { "id": 1, "name": "Q2 All-Hands", "date": "2024-05-15", "type": "Meeting", "description": "string" }
    ]
  }

### POST /events
Request: { "name": "string", "date": "YYYY-MM-DD", "type": "Meeting|Event|Social", "description": "string" }
Response: (created event with id)

### PUT /events/:id
Request: (same as POST)
Response: (updated event)

### DELETE /events/:id
Response: { "success": true }

---

## DASHBOARD

### GET /dashboard/admin
Response:
  {
    "totalEmployees": 7,
    "totalProjects": 4,
    "activeProjects": 3,
    "pendingLeaves": 2,
    "totalDepartments": 4
  }

### GET /dashboard/employee/:employeeId
Response:
  {
    "hoursThisMonth": 23,
    "leavesTaken": 2.5,
    "activeProjects": 2,
    "attendanceRate": 96
  }

---

## SEARCH

### GET /search
Query params: q (string, min 2 chars)
Response:
  {
    "data": [
      { "type": "employee", "id": 1, "label": "Priya Sharma", "sub": "Software Engineer", "avatar": "PS", "color": "#7c3aed" },
      { "type": "project",  "id": 1, "label": "Nexus Platform v2.0", "sub": "active" }
    ]
  }

---

## NOTIFICATIONS

### GET /notifications
Response:
  {
    "data": [
      { "id": 1, "text": "Your leave was approved", "time": "10 min ago", "read": false }
    ]
  }

### PATCH /notifications/:id/read
Response: { "success": true }

### POST /notifications/read-all
Response: { "success": true }

---

## .NET INTEGRATION NOTES

1. JWT Auth:
   - Client sends: Authorization: Bearer <token>
   - Configure in Program.cs:
     builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => { ... });

2. CORS:
   - Allow: http://localhost:5500 (or wherever you serve the frontend)
   - builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
       p.WithOrigins("http://localhost:5500").AllowAnyHeader().AllowAnyMethod()));

3. Response Envelope:
   - All responses wrap data in { success: true, data: ... }
   - Create a generic ApiResponse<T> wrapper in C#:
     public class ApiResponse<T> { public bool Success { get; set; } public T Data { get; set; } public string Message { get; set; } }

4. File Uploads:
   - /employees/:id/avatar and /policies/upload use multipart/form-data
   - Use IFormFile in controller action parameter

5. Pagination:
   - Return { data: [], total: N } — client handles page/pageSize calculation
   - Recommended: accept page & pageSize query params, apply .Skip().Take() in EF Core

6. Switching from Mock to Real API:
   - Open js/services/api.js
   - Set: window.MOCK_MODE = false;  (line near top of file)
   - Set BASE_URL to your deployed .NET API URL
   - Remove the entire mockHandler function and MOCK_MODE assignment
