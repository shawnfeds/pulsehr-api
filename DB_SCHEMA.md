# NexusHR v2 — MS SQL Server Schema

This file documents the recommended database schema for the NexusHR project based on the frontend UI and API contract.

## Tables

### Employees
- EmployeeId (PK)
- Name
- Email
- PasswordHash
- PasswordSalt
- IsAdmin / UserType
- Role
- Dept
- Status (active / inactive)
- JoinDate
- Phone
- Location
- AvatarText
- AvatarColor
- AvatarUrl
- CreatedAt
- UpdatedAt

### Roles
- RoleId (PK)
- Name
- Description

### EmployeeRoles
- EmployeeRoleId (PK)
- EmployeeId (FK -> Employees)
- RoleId (FK -> Roles)

### Projects
- ProjectId (PK)
- Name
- Description
- ManagerEmployeeId (FK -> Employees)
- Status (active / on-hold / completed)
- StartDate
- EndDate
- Progress
- CreatedAt
- UpdatedAt

### ProjectMembers
- ProjectMemberId (PK)
- ProjectId (FK -> Projects)
- EmployeeId (FK -> Employees)
- AssignedOn

### Leaves
- LeaveId (PK)
- EmployeeId (FK -> Employees)
- LeaveType (Sick / Casual / etc.)
- StartDate
- EndDate
- Days
- HalfDay
- Reason
- Status (pending / approved / rejected)
- Remarks
- CreatedAt
- UpdatedAt

### LeaveBalances
- LeaveBalanceId (PK)
- EmployeeId (FK -> Employees)
- LeaveType
- Total
- Used
- Balance

### TimesheetEntries
- TimesheetId (PK)
- EmployeeId (FK -> Employees)
- ProjectId (FK -> Projects)
- Date
- Task
- Hours
- Month
- CreatedAt
- UpdatedAt

### Policies
- PolicyId (PK)
- Name
- Category
- UploadedByEmployeeId (FK -> Employees)
- UploadedOn
- FileSize
- ContentType
- FilePath
- Notes

### Holidays
- HolidayId (PK)
- Name
- Date
- Type (National / Festival / Optional / etc.)
- Description

### Events
- EventId (PK)
- Name
- Description
- Date
- Type (Meeting / Event / Social / etc.)
- Location

### SalaryHistory
- SalaryHistoryId (PK)
- EmployeeId (FK -> Employees)
- Date
- Amount
- Note

### EmploymentHistory
- EmploymentHistoryId (PK)
- EmployeeId (FK -> Employees)
- Date
- Event
- Role
- Dept
- Notes

### Notifications (optional)
- NotificationId (PK)
- EmployeeId (FK -> Employees)
- Title
- Body
- IsRead
- CreatedAt

### RefreshTokens (optional)
- RefreshTokenId (PK)
- EmployeeId (FK -> Employees)
- Token
- ExpiresAt
- CreatedAt
- RevokedAt

## Relationships
- Employees -> ProjectMembers: one-to-many
- Projects -> ProjectMembers: one-to-many
- Employees -> Leaves: one-to-many
- Employees -> TimesheetEntries: one-to-many
- Employees -> Policies: one-to-many
- Employees -> SalaryHistory: one-to-many
- Employees -> EmploymentHistory: one-to-many
- Employees -> LeaveBalances: one-to-many
- Employees -> Notifications: one-to-many
- Employees -> RefreshTokens: one-to-many
- Roles -> EmployeeRoles: one-to-many

## Notes
- `Employees` contains both regular users and admins; use `IsAdmin` or `UserType` for role tier.
- `Dept` can be a string, but `Departments` can be added if normalization is desired.
- `LeaveBalances` can be derived, but storing it improves performance for balance queries.
- `Policies` should store file metadata and path rather than document content in the DB if files are stored on disk or cloud.
- `ProjectMembers` is required for many-to-many employee/project assignments.
