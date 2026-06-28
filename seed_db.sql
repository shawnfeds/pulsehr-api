-- ============================================================
-- PulseHR Seeding Script
-- Run this AFTER create_db.sql to seed initial test users
-- ============================================================

USE PulseHR;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Ensure Roles are seeded (already seeded in create_db.sql, but just in case)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Admin')
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES ('Admin', 'System administrator with full access');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Employee')
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES ('Employee', 'Standard employee with self-service access');
GO

-- 2. Seed Default Admin User
-- Password: AdminPassword123!
-- BCrypt Hash: $2a$11$qR3mO528r20Jm6o2yLdWeeM0wBvHlhjD4QvA6B97R9xK6a8gL5xUq
IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Email] = 'admin@pulsehr.com')
BEGIN
    INSERT INTO [dbo].[Employees] (
        [Name], [Email], [PasswordHash], [PasswordSalt], [IsAdmin], [UserType], 
        [Role], [Dept], [Status], [JoinDate], [Phone], [Location], [AvatarText], [AvatarColor]
    ) VALUES (
        'Administrator', 
        'admin@pulsehr.com', 
        '$2a$11$qR3mO528r20Jm6o2yLdWeeM0wBvHlhjD4QvA6B97R9xK6a8gL5xUq', 
        '', 
        1, 
        'Admin', 
        'System Admin', 
        'IT', 
        'active', 
        '2026-01-01', 
        '+1 555 0199', 
        'New York, NY', 
        'AD', 
        '#dc2626'
    );

    -- Assign Admin role to Employee
    DECLARE @AdminEmpId INT = (SELECT [EmployeeId] FROM [dbo].[Employees] WHERE [Email] = 'admin@pulsehr.com');
    DECLARE @AdminRoleId INT = (SELECT [RoleId] FROM [dbo].[Roles] WHERE [Name] = 'Admin');
    
    INSERT INTO [dbo].[EmployeeRoles] ([EmployeeId], [RoleId]) VALUES (@AdminEmpId, @AdminRoleId);

    -- Seed leave balances for Admin
    INSERT INTO [dbo].[LeaveBalances] ([EmployeeId], [LeaveType], [Total], [Used]) VALUES 
        (@AdminEmpId, 'Sick', 12.0, 0.0),
        (@AdminEmpId, 'Casual', 12.0, 0.0);
END
GO

-- 3. Seed Default Employee User
-- Password: EmployeePassword123!
-- BCrypt Hash: $2a$11$0F/oM5eJswK9c64Jt3bO7O.lC5eNq5D1jV9aVf6Zk9XyS7t5T.Z6i
IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Email] = 'priya@pulsehr.com')
BEGIN
    INSERT INTO [dbo].[Employees] (
        [Name], [Email], [PasswordHash], [PasswordSalt], [IsAdmin], [UserType], 
        [Role], [Dept], [Status], [JoinDate], [Phone], [Location], [AvatarText], [AvatarColor]
    ) VALUES (
        'Priya Sharma', 
        'priya@pulsehr.com', 
        '$2a$11$0F/oM5eJswK9c64Jt3bO7O.lC5eNq5D1jV9aVf6Zk9XyS7t5T.Z6i', 
        '', 
        0, 
        'Employee', 
        'Software Engineer', 
        'Engineering', 
        'active', 
        '2026-02-15', 
        '+91 98765 43210', 
        'Bengaluru, KA', 
        'PS', 
        '#7c3aed'
    );

    -- Assign Employee role
    DECLARE @EmpId INT = (SELECT [EmployeeId] FROM [dbo].[Employees] WHERE [Email] = 'priya@pulsehr.com');
    DECLARE @EmpRoleId INT = (SELECT [RoleId] FROM [dbo].[Roles] WHERE [Name] = 'Employee');
    
    INSERT INTO [dbo].[EmployeeRoles] ([EmployeeId], [RoleId]) VALUES (@EmpId, @EmpRoleId);

    -- Seed leave balances for Employee
    INSERT INTO [dbo].[LeaveBalances] ([EmployeeId], [LeaveType], [Total], [Used]) VALUES 
        (@EmpId, 'Sick', 12.0, 0.0),
        (@EmpId, 'Casual', 12.0, 0.0);
END
GO

PRINT 'Seeding completed. Initial accounts created:';
PRINT '  - Admin: admin@pulsehr.com (Password: AdminPassword123!)';
PRINT '  - Employee: priya@pulsehr.com (Password: EmployeePassword123!)';
GO
