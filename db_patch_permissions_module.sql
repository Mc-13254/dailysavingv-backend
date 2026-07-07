-- Patch: create the Permission/RolePermission module (RBAC matrix)
-- and seed the fixed permission catalog.

CREATE TABLE Permission (
    PermissionID    INT IDENTITY(1,1) PRIMARY KEY,
    PermissionCode  NVARCHAR(60)  NOT NULL UNIQUE,
    PermissionName  NVARCHAR(100) NOT NULL,
    Module          NVARCHAR(50)  NOT NULL,
    Action          NVARCHAR(30)  NOT NULL,
    Description     NVARCHAR(300) NULL
);
GO

CREATE TABLE RolePermission (
    RolePermissionID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID           INT NOT NULL REFERENCES Role(RoleID) ON DELETE CASCADE,
    PermissionID     INT NOT NULL REFERENCES Permission(PermissionID) ON DELETE CASCADE,
    Allowed          BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_RolePermission UNIQUE (RoleID, PermissionID)
);
GO

INSERT INTO Permission (PermissionCode, PermissionName, Module, Action) VALUES
('DASHBOARD_VIEW','View Dashboard','Dashboard','VIEW'),
('IMF_VIEW','View','IMF','VIEW'),
('IMF_CREATE','Create','IMF','CREATE'),
('IMF_EDIT','Edit','IMF','EDIT'),
('IMF_DELETE','Delete','IMF','DELETE'),
('AGENCY_VIEW','View','Agency','VIEW'),
('AGENCY_CREATE','Create','Agency','CREATE'),
('AGENCY_EDIT','Edit','Agency','EDIT'),
('AGENCY_DELETE','Delete','Agency','DELETE'),
('DEPARTMENT_VIEW','View','Department','VIEW'),
('DEPARTMENT_CREATE','Create','Department','CREATE'),
('DEPARTMENT_EDIT','Edit','Department','EDIT'),
('DEPARTMENT_DELETE','Delete','Department','DELETE'),
('USERS_VIEW','View','Users','VIEW'),
('USERS_CREATE','Create','Users','CREATE'),
('USERS_EDIT','Edit','Users','EDIT'),
('USERS_DELETE','Delete','Users','DELETE'),
('ROLES_VIEW','View','Roles','VIEW'),
('ROLES_CREATE','Create','Roles','CREATE'),
('ROLES_EDIT','Edit','Roles','EDIT'),
('ROLES_DELETE','Delete','Roles','DELETE'),
('CONTRACT_TYPES_VIEW','View','Contract Types','VIEW'),
('CONTRACT_TYPES_CREATE','Create','Contract Types','CREATE'),
('CONTRACT_TYPES_EDIT','Edit','Contract Types','EDIT'),
('CONTRACT_TYPES_DELETE','Delete','Contract Types','DELETE'),
('COMMISSION_TYPES_VIEW','View','Commission Types','VIEW'),
('COMMISSION_TYPES_CREATE','Create','Commission Types','CREATE'),
('COMMISSION_TYPES_EDIT','Edit','Commission Types','EDIT'),
('COMMISSION_TYPES_DELETE','Delete','Commission Types','DELETE'),
('COMMISSION_RANGES_VIEW','View','Commission Ranges','VIEW'),
('COMMISSION_RANGES_CREATE','Create','Commission Ranges','CREATE'),
('COMMISSION_RANGES_EDIT','Edit','Commission Ranges','EDIT'),
('COMMISSION_RANGES_DELETE','Delete','Commission Ranges','DELETE'),
('NUMBERING_PARAMETERS_VIEW','View','Numbering Parameters','VIEW'),
('NUMBERING_PARAMETERS_CREATE','Create','Numbering Parameters','CREATE'),
('NUMBERING_PARAMETERS_EDIT','Edit','Numbering Parameters','EDIT'),
('NUMBERING_PARAMETERS_DELETE','Delete','Numbering Parameters','DELETE'),
('COLLECTORS_VIEW','View','Collectors','VIEW'),
('COLLECTORS_CREATE','Create','Collectors','CREATE'),
('COLLECTORS_EDIT','Edit','Collectors','EDIT'),
('COLLECTORS_DELETE','Delete','Collectors','DELETE'),
('COLLECTORS_APPROVE','Approve','Collectors','APPROVE'),
('CLIENTS_VIEW','View','Clients','VIEW'),
('CLIENTS_CREATE','Create','Clients','CREATE'),
('CLIENTS_EDIT','Edit','Clients','EDIT'),
('CLIENTS_DELETE','Delete','Clients','DELETE'),
('CLIENTS_APPROVE','Approve','Clients','APPROVE'),
('OPERATIONS_DAILY_COLLECTIONS','Daily Collections','Operations','DAILY_COLLECTIONS'),
('OPERATIONS_DEPOSITS','Deposits','Operations','DEPOSITS'),
('OPERATIONS_WITHDRAWALS','Withdrawals','Operations','WITHDRAWALS'),
('OPERATIONS_TRANSFERS','Transfers','Operations','TRANSFERS'),
('OPERATIONS_VALIDATION','Validation','Operations','VALIDATION'),
('OPERATIONS_REVERSE','Reverse Transaction','Operations','REVERSE'),
('REPORTS_VIEW','View Reports','Reports','VIEW'),
('REPORTS_EXPORT','Export Reports','Reports','EXPORT'),
('REPORTS_FINANCIAL','Financial Reports','Reports','FINANCIAL'),
('SECURITY_AUDIT_LOGS','Audit Logs','Security','AUDIT_LOGS'),
('SECURITY_ACTIVITY_LOGS','Activity Logs','Security','ACTIVITY_LOGS'),
('SECURITY_LOGIN_HISTORY','Login History','Security','LOGIN_HISTORY');
GO

-- Administrator role always has full permissions.
INSERT INTO RolePermission (RoleID, PermissionID, Allowed)
SELECT r.RoleID, p.PermissionID, 1
FROM Role r
CROSS JOIN Permission p
WHERE r.Code = 'ADMIN';
GO
