-- ============================================================
-- PulseHR Seeding Script (PostgreSQL Version)
-- Run this to seed initial test users in PostgreSQL/Neon DB
-- ============================================================

-- 1. Ensure Roles are seeded
INSERT INTO "Roles" ("Name", "Description")
VALUES 
    ('Admin', 'System administrator with full access'),
    ('Employee', 'Standard employee with self-service access')
ON CONFLICT ("Name") DO NOTHING;

-- 2. Seed Default Admin User
-- Password: AdminPassword123!
-- BCrypt Hash: $2a$11$qR3mO528r20Jm6o2yLdWeeM0wBvHlhjD4QvA6B97R9xK6a8gL5xUq
INSERT INTO "Employees" (
    "Name", "Email", "PasswordHash", "PasswordSalt", "IsAdmin", "UserType", 
    "Role", "Dept", "Status", "JoinDate", "Phone", "Location", "AvatarText", "AvatarColor", "CreatedAt", "UpdatedAt"
) VALUES (
    'Administrator', 
    'admin@pulsehr.com', 
    '$2a$11$qR3mO528r20Jm6o2yLdWeeM0wBvHlhjD4QvA6B97R9xK6a8gL5xUq', 
    '', 
    true, 
    'Admin', 
    'System Admin', 
    'IT', 
    'active', 
    '2026-01-01', 
    '+1 555 0199', 
    'New York, NY', 
    'AD', 
    '#dc2626',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Email") DO NOTHING;

-- Assign Admin role to Employee
INSERT INTO "EmployeeRoles" ("EmployeeId", "RoleId")
SELECT e."EmployeeId", r."RoleId"
FROM "Employees" e
CROSS JOIN "Roles" r
WHERE e."Email" = 'admin@pulsehr.com' AND r."Name" = 'Admin'
ON CONFLICT ("EmployeeId", "RoleId") DO NOTHING;

-- Seed leave balances for Admin
INSERT INTO "LeaveBalances" ("EmployeeId", "LeaveType", "Total", "Used")
SELECT e."EmployeeId", val.leave_type, val.total, val.used
FROM "Employees" e
CROSS JOIN (
    VALUES 
        ('Sick', 12.0, 0.0),
        ('Casual', 12.0, 0.0)
) AS val(leave_type, total, used)
WHERE e."Email" = 'admin@pulsehr.com'
ON CONFLICT ("EmployeeId", "LeaveType") DO NOTHING;


-- 3. Seed Default Employee User
-- Password: EmployeePassword123!
-- BCrypt Hash: $2a$11$0F/oM5eJswK9c64Jt3bO7O.lC5eNq5D1jV9aVf6Zk9XyS7t5T.Z6i
INSERT INTO "Employees" (
    "Name", "Email", "PasswordHash", "PasswordSalt", "IsAdmin", "UserType", 
    "Role", "Dept", "Status", "JoinDate", "Phone", "Location", "AvatarText", "AvatarColor", "CreatedAt", "UpdatedAt"
) VALUES (
    'Priya Sharma', 
    'priya@pulsehr.com', 
    '$2a$11$0F/oM5eJswK9c64Jt3bO7O.lC5eNq5D1jV9aVf6Zk9XyS7t5T.Z6i', 
    '', 
    false, 
    'Employee', 
    'Software Engineer', 
    'Engineering', 
    'active', 
    '2026-02-15', 
    '+91 98765 43210', 
    'Bengaluru, KA', 
    'PS', 
    '#7c3aed',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Email") DO NOTHING;

-- Assign Employee role
INSERT INTO "EmployeeRoles" ("EmployeeId", "RoleId")
SELECT e."EmployeeId", r."RoleId"
FROM "Employees" e
CROSS JOIN "Roles" r
WHERE e."Email" = 'priya@pulsehr.com' AND r."Name" = 'Employee'
ON CONFLICT ("EmployeeId", "RoleId") DO NOTHING;

-- Seed leave balances for Employee
INSERT INTO "LeaveBalances" ("EmployeeId", "LeaveType", "Total", "Used")
SELECT e."EmployeeId", val.leave_type, val.total, val.used
FROM "Employees" e
CROSS JOIN (
    VALUES 
        ('Sick', 12.0, 0.0),
        ('Casual', 12.0, 0.0)
) AS val(leave_type, total, used)
WHERE e."Email" = 'priya@pulsehr.com'
ON CONFLICT ("EmployeeId", "LeaveType") DO NOTHING;
