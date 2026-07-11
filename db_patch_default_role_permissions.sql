-- Patch: sensible default module permissions for CASHIER, SUPERVISOR, MANAGER,
-- COLLECTOR roles — without this, only ADMIN had any permission granted at
-- all, so every other role's sidebar was correctly-but-uselessly empty.
-- An administrator can fine-tune these later via Permission Management.
-- IDEMPOTENT: safe to re-run.

DECLARE @RoleModules TABLE (RoleCode NVARCHAR(20), Module NVARCHAR(50));

INSERT INTO @RoleModules (RoleCode, Module) VALUES
-- Cashier: day-to-day teller operations only.
('CASHIER', 'Dashboard'), ('CASHIER', 'Operations'), ('CASHIER', 'Clients'), ('CASHIER', 'CashSession'), ('CASHIER', 'Teller'), ('CASHIER', 'Documents'), ('CASHIER', 'Notifications'),
-- Supervisor: operations + approvals + reporting oversight.
('SUPERVISOR', 'Dashboard'), ('SUPERVISOR', 'Operations'), ('SUPERVISOR', 'Clients'), ('SUPERVISOR', 'Collectors'), ('SUPERVISOR', 'CashSession'), ('SUPERVISOR', 'Teller'),
('SUPERVISOR', 'Loans'), ('SUPERVISOR', 'Reports'), ('SUPERVISOR', 'Documents'), ('SUPERVISOR', 'Notifications'), ('SUPERVISOR', 'FraudDetection'),
-- Manager: broad operational + reporting + executive visibility, not system administration.
('MANAGER', 'Dashboard'), ('MANAGER', 'ExecutiveDashboard'), ('MANAGER', 'Operations'), ('MANAGER', 'Clients'), ('MANAGER', 'Collectors'), ('MANAGER', 'CashSession'), ('MANAGER', 'Teller'),
('MANAGER', 'Loans'), ('MANAGER', 'Reports'), ('MANAGER', 'Accounting'), ('MANAGER', 'Documents'), ('MANAGER', 'Notifications'), ('MANAGER', 'FraudDetection'), ('MANAGER', 'Agency'),
-- Collector: field collection work.
('COLLECTOR', 'Dashboard'), ('COLLECTOR', 'Operations'), ('COLLECTOR', 'Clients'), ('COLLECTOR', 'Notifications');

INSERT INTO RolePermission (RoleID, PermissionID, Allowed)
SELECT r.RoleID, p.PermissionID, 1
FROM @RoleModules rm
INNER JOIN Role r ON r.RoleType = rm.RoleCode
INNER JOIN Permission p ON p.Module = rm.Module
WHERE NOT EXISTS (SELECT 1 FROM RolePermission rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);
GO
