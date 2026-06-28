-- ============================================================
-- PulseHR Database Creation Script
-- SQL Server — Run this BEFORE scaffolding with EF Core
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PulseHR')
BEGIN
    CREATE DATABASE PulseHR;
END
GO

USE PulseHR;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- 1. ROLES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]      INT           NOT NULL IDENTITY(1,1),
        [Name]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId]),
        CONSTRAINT [UQ_Roles_Name] UNIQUE ([Name])
    );
END
GO

-- ============================================================
-- 2. EMPLOYEES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Employees] (
        [EmployeeId]   INT            NOT NULL IDENTITY(1,1),
        [Name]         NVARCHAR(200)  NOT NULL,
        [Email]        NVARCHAR(256)  NOT NULL,
        [PasswordHash] NVARCHAR(512)  NOT NULL,
        [PasswordSalt] NVARCHAR(512)  NOT NULL,
        [IsAdmin]      BIT            NOT NULL DEFAULT 0,
        [UserType]     NVARCHAR(50)   NOT NULL DEFAULT 'Employee',
        [Role]         NVARCHAR(200)  NULL,
        [Dept]         NVARCHAR(200)  NULL,
        [Status]       NVARCHAR(20)   NOT NULL DEFAULT 'active',
        [JoinDate]     DATE           NULL,
        [Phone]        NVARCHAR(50)   NULL,
        [Location]     NVARCHAR(200)  NULL,
        [AvatarText]   NVARCHAR(10)   NULL,
        [AvatarColor]  NVARCHAR(20)   NULL,
        [AvatarUrl]    NVARCHAR(500)  NULL,
        [Salary]       DECIMAL(18,2)  NULL,
        [CreatedAt]    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([EmployeeId]),
        CONSTRAINT [UQ_Employees_Email] UNIQUE ([Email]),
        CONSTRAINT [CK_Employees_Status]   CHECK ([Status]   IN ('active', 'inactive')),
        CONSTRAINT [CK_Employees_UserType] CHECK ([UserType] IN ('Employee', 'Admin'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Email')
    CREATE INDEX [IX_Employees_Email]  ON [dbo].[Employees] ([Email]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Status')
    CREATE INDEX [IX_Employees_Status] ON [dbo].[Employees] ([Status]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Dept')
    CREATE INDEX [IX_Employees_Dept]   ON [dbo].[Employees] ([Dept]);
GO

-- ============================================================
-- 3. EMPLOYEE ROLES  (many-to-many)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeRoles]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[EmployeeRoles] (
        [EmployeeRoleId] INT NOT NULL IDENTITY(1,1),
        [EmployeeId]     INT NOT NULL,
        [RoleId]         INT NOT NULL,
        CONSTRAINT [PK_EmployeeRoles]          PRIMARY KEY CLUSTERED ([EmployeeRoleId]),
        CONSTRAINT [FK_EmployeeRoles_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeRoles_Role]     FOREIGN KEY ([RoleId])     REFERENCES [dbo].[Roles]([RoleId])         ON DELETE CASCADE,
        CONSTRAINT [UQ_EmployeeRoles]          UNIQUE ([EmployeeId], [RoleId])
    );
END
GO

-- ============================================================
-- 4. PROJECTS
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Projects] (
        [ProjectId]         INT            NOT NULL IDENTITY(1,1),
        [Name]              NVARCHAR(300)  NOT NULL,
        [Description]       NVARCHAR(2000) NULL,
        [ManagerEmployeeId] INT            NULL,
        [Status]            NVARCHAR(20)   NOT NULL DEFAULT 'active',
        [StartDate]         DATE           NULL,
        [EndDate]           DATE           NULL,
        [Progress]          INT            NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Projects]          PRIMARY KEY CLUSTERED ([ProjectId]),
        CONSTRAINT [FK_Projects_Manager]  FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE SET NULL,
        CONSTRAINT [CK_Projects_Status]   CHECK ([Status]   IN ('active', 'on-hold', 'completed')),
        CONSTRAINT [CK_Projects_Progress] CHECK ([Progress] BETWEEN 0 AND 100)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Projects_Status')
    CREATE INDEX [IX_Projects_Status] ON [dbo].[Projects] ([Status]);
GO

-- ============================================================
-- 5. PROJECT MEMBERS  (many-to-many)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProjectMembers]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[ProjectMembers] (
        [ProjectMemberId] INT       NOT NULL IDENTITY(1,1),
        [ProjectId]       INT       NOT NULL,
        [EmployeeId]      INT       NOT NULL,
        [AssignedOn]      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProjectMembers]         PRIMARY KEY CLUSTERED ([ProjectMemberId]),
        CONSTRAINT [FK_ProjectMembers_Project]  FOREIGN KEY ([ProjectId])  REFERENCES [dbo].[Projects]([ProjectId])   ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectMembers_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_ProjectMembers]          UNIQUE ([ProjectId], [EmployeeId])
    );
END
GO

-- ============================================================
-- 6. LEAVES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Leaves]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Leaves] (
        [LeaveId]    INT            NOT NULL IDENTITY(1,1),
        [EmployeeId] INT            NOT NULL,
        [LeaveType]  NVARCHAR(50)   NOT NULL,
        [StartDate]  DATE           NOT NULL,
        [EndDate]    DATE           NOT NULL,
        [Days]       DECIMAL(5,1)   NOT NULL,
        [HalfDay]    BIT            NOT NULL DEFAULT 0,
        [Reason]     NVARCHAR(1000) NULL,
        [Status]     NVARCHAR(20)   NOT NULL DEFAULT 'pending',
        [Remarks]    NVARCHAR(500)  NULL,
        [CreatedAt]  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Leaves]          PRIMARY KEY CLUSTERED ([LeaveId]),
        CONSTRAINT [FK_Leaves_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE,
        CONSTRAINT [CK_Leaves_Status]   CHECK ([Status] IN ('pending', 'approved', 'rejected')),
        CONSTRAINT [CK_Leaves_Days]     CHECK ([Days] > 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leaves_EmployeeId')
    CREATE INDEX [IX_Leaves_EmployeeId] ON [dbo].[Leaves] ([EmployeeId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leaves_Status')
    CREATE INDEX [IX_Leaves_Status]     ON [dbo].[Leaves] ([Status]);
GO

-- ============================================================
-- 7. LEAVE BALANCES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LeaveBalances]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[LeaveBalances] (
        [LeaveBalanceId] INT          NOT NULL IDENTITY(1,1),
        [EmployeeId]     INT          NOT NULL,
        [LeaveType]      NVARCHAR(50) NOT NULL,
        [Total]          DECIMAL(5,1) NOT NULL DEFAULT 12,
        [Used]           DECIMAL(5,1) NOT NULL DEFAULT 0,
        [Balance]        AS ([Total] - [Used]) PERSISTED,
        CONSTRAINT [PK_LeaveBalances]          PRIMARY KEY CLUSTERED ([LeaveBalanceId]),
        CONSTRAINT [FK_LeaveBalances_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE,
        CONSTRAINT [UQ_LeaveBalances]          UNIQUE ([EmployeeId], [LeaveType]),
        CONSTRAINT [CK_LeaveBalances_Used]     CHECK ([Used] >= 0)
    );
END
GO

-- ============================================================
-- 8. TIMESHEET ENTRIES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TimesheetEntries]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[TimesheetEntries] (
        [TimesheetId] INT           NOT NULL IDENTITY(1,1),
        [EmployeeId]  INT           NOT NULL,
        [ProjectId]   INT           NOT NULL,
        [Date]        DATE          NOT NULL,
        [Task]        NVARCHAR(500) NOT NULL,
        [Hours]       DECIMAL(5,2)  NOT NULL,
        [Month]       NVARCHAR(30)  NOT NULL,
        [CreatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_TimesheetEntries]   PRIMARY KEY CLUSTERED ([TimesheetId]),
        CONSTRAINT [FK_Timesheet_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Timesheet_Project]  FOREIGN KEY ([ProjectId])  REFERENCES [dbo].[Projects]([ProjectId])  ON DELETE NO ACTION,
        CONSTRAINT [CK_Timesheet_Hours]    CHECK ([Hours] > 0 AND [Hours] <= 24)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Timesheet_EmployeeId')
    CREATE INDEX [IX_Timesheet_EmployeeId] ON [dbo].[TimesheetEntries] ([EmployeeId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Timesheet_Month')
    CREATE INDEX [IX_Timesheet_Month]      ON [dbo].[TimesheetEntries] ([Month]);
GO

-- ============================================================
-- 9. POLICIES
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Policies]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Policies] (
        [PolicyId]             INT            NOT NULL IDENTITY(1,1),
        [Name]                 NVARCHAR(300)  NOT NULL,
        [Category]             NVARCHAR(100)  NOT NULL,
        [UploadedByEmployeeId] INT            NULL,
        [UploadedOn]           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [FileSize]             NVARCHAR(50)   NULL,
        [ContentType]          NVARCHAR(100)  NULL,
        [FilePath]             NVARCHAR(1000) NOT NULL,
        [Notes]                NVARCHAR(1000) NULL,
        CONSTRAINT [PK_Policies]           PRIMARY KEY CLUSTERED ([PolicyId]),
        CONSTRAINT [FK_Policies_UploadedBy] FOREIGN KEY ([UploadedByEmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE SET NULL
    );
END
GO

-- ============================================================
-- 10. HOLIDAYS
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Holidays]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Holidays] (
        [HolidayId]   INT           NOT NULL IDENTITY(1,1),
        [Name]        NVARCHAR(200) NOT NULL,
        [Date]        DATE          NOT NULL,
        [Type]        NVARCHAR(50)  NOT NULL DEFAULT 'National',
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY CLUSTERED ([HolidayId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Holidays_Date')
    CREATE INDEX [IX_Holidays_Date] ON [dbo].[Holidays] ([Date]);
GO

-- ============================================================
-- 11. EVENTS
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Events] (
        [EventId]     INT            NOT NULL IDENTITY(1,1),
        [Name]        NVARCHAR(200)  NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [Date]        DATETIME2      NOT NULL,
        [Type]        NVARCHAR(50)   NOT NULL DEFAULT 'Event',
        [Location]    NVARCHAR(300)  NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED ([EventId])
    );
END
GO

-- ============================================================
-- 12. SALARY HISTORY
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SalaryHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[SalaryHistory] (
        [SalaryHistoryId] INT           NOT NULL IDENTITY(1,1),
        [EmployeeId]      INT           NOT NULL,
        [Date]            DATE          NOT NULL,
        [Amount]          DECIMAL(18,2) NOT NULL,
        [Note]            NVARCHAR(500) NULL,
        CONSTRAINT [PK_SalaryHistory]          PRIMARY KEY CLUSTERED ([SalaryHistoryId]),
        CONSTRAINT [FK_SalaryHistory_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- 13. EMPLOYMENT HISTORY
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmploymentHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[EmploymentHistory] (
        [EmploymentHistoryId] INT            NOT NULL IDENTITY(1,1),
        [EmployeeId]          INT            NOT NULL,
        [Date]                DATE           NOT NULL,
        [Event]               NVARCHAR(200)  NOT NULL,
        [Role]                NVARCHAR(200)  NULL,
        [Dept]                NVARCHAR(200)  NULL,
        [Notes]               NVARCHAR(1000) NULL,
        CONSTRAINT [PK_EmploymentHistory]          PRIMARY KEY CLUSTERED ([EmploymentHistoryId]),
        CONSTRAINT [FK_EmploymentHistory_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- 14. NOTIFICATIONS
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT            NOT NULL IDENTITY(1,1),
        [EmployeeId]     INT            NOT NULL,
        [Title]          NVARCHAR(300)  NOT NULL,
        [Body]           NVARCHAR(1000) NULL,
        [IsRead]         BIT            NOT NULL DEFAULT 0,
        [CreatedAt]      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Notifications]          PRIMARY KEY CLUSTERED ([NotificationId]),
        CONSTRAINT [FK_Notifications_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_EmployeeId')
    CREATE INDEX [IX_Notifications_EmployeeId] ON [dbo].[Notifications] ([EmployeeId]);
GO

-- ============================================================
-- 15. REFRESH TOKENS
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [RefreshTokenId]  INT            NOT NULL IDENTITY(1,1),
        [EmployeeId]      INT            NOT NULL,
        [Token]           NVARCHAR(512)  NOT NULL,
        [ExpiresAt]       DATETIME2      NOT NULL,
        [CreatedAt]       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [RevokedAt]       DATETIME2      NULL,
        [ReplacedByToken] NVARCHAR(512)  NULL,
        [CreatedByIp]     NVARCHAR(50)   NULL,
        [RevokedByIp]     NVARCHAR(50)   NULL,
        CONSTRAINT [PK_RefreshTokens]          PRIMARY KEY CLUSTERED ([RefreshTokenId]),
        CONSTRAINT [FK_RefreshTokens_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([EmployeeId]) ON DELETE CASCADE,
        CONSTRAINT [UQ_RefreshTokens_Token]    UNIQUE ([Token])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token')
    CREATE INDEX [IX_RefreshTokens_Token]      ON [dbo].[RefreshTokens] ([Token]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_EmployeeId')
    CREATE INDEX [IX_RefreshTokens_EmployeeId] ON [dbo].[RefreshTokens] ([EmployeeId]);
GO

-- ============================================================
-- SEED: Default Roles
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles])
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES
        ('Employee',  'Standard employee with self-service access'),
        ('TeamLead',  'Team lead with limited team management rights'),
        ('Manager',   'Department manager'),
        ('HR',        'HR staff with full leave and employee management'),
        ('Admin',     'System administrator with full access');
END
GO

PRINT 'PulseHR schema created successfully.';
GO
