-- Patch: register permissions for every module added since the original
-- Permission Management seed (Loans, Teller, Documents, Notifications,
-- Accounting, Fraud Detection, Executive Dashboard, extra Security pages).
-- IDEMPOTENT: safe to re-run. Grants ADMIN full access to all new permissions.

DECLARE @NewPermissions TABLE (PermissionCode NVARCHAR(50), PermissionName NVARCHAR(50), Module NVARCHAR(50), Action NVARCHAR(20));

INSERT INTO @NewPermissions (PermissionCode, PermissionName, Module, Action) VALUES
('LOANS_VIEW','View','Loans','VIEW'),('LOANS_CREATE','Create','Loans','CREATE'),('LOANS_APPROVE','Approve','Loans','APPROVE'),('LOANS_DISBURSE','Disburse','Loans','DISBURSE'),('LOANS_REPAY','Repay','Loans','REPAY'),
('TELLER_VIEW','View','Teller','VIEW'),('TELLER_REQUEST','Request Movement','Teller','CREATE'),('TELLER_APPROVE','Approve Movement','Teller','APPROVE'),
('DOCUMENTS_VIEW','View','Documents','VIEW'),('DOCUMENTS_UPLOAD','Upload','Documents','CREATE'),('DOCUMENTS_DELETE','Delete','Documents','DELETE'),
('NOTIFICATIONS_VIEW','View','Notifications','VIEW'),
('ACCOUNTING_VIEW','View','Accounting','VIEW'),('ACCOUNTING_MANUAL_ENTRY','Manual Entry','Accounting','CREATE'),('ACCOUNTING_CLOSE_PERIOD','Close Period','Accounting','APPROVE'),('ACCOUNTING_REVERSE','Reverse Entry','Accounting','DELETE'),
('FRAUD_VIEW','View','FraudDetection','VIEW'),('FRAUD_REVIEW','Review','FraudDetection','APPROVE'),
('EXECUTIVE_DASHBOARD_VIEW','View','ExecutiveDashboard','VIEW'),
('SECURITY_SESSIONS_VIEW','View Active Sessions','Security','VIEW'),('SECURITY_SESSIONS_TERMINATE','Terminate Session','Security','DELETE'),
('SECURITY_FAILED_LOGINS_VIEW','View Failed Logins','Security','VIEW'),
('SECURITY_LOCKOUT_MANAGE','Manage Lockouts','Security','EDIT'),
('SECURITY_PASSWORD_POLICY','Password Policy','Security','EDIT'),
('SECURITY_API_KEYS','API Keys','Security','EDIT'),
('SECURITY_SYSTEM_HEALTH','System Health','Security','VIEW'),
('CASH_SESSION_VIEW','View','CashSession','VIEW'),('CASH_SESSION_OPEN','Open','CashSession','CREATE'),('CASH_SESSION_CLOSE','Close','CashSession','EDIT');

INSERT INTO Permission (PermissionCode, PermissionName, Module, Action)
SELECT np.PermissionCode, np.PermissionName, np.Module, np.Action
FROM @NewPermissions np
WHERE NOT EXISTS (SELECT 1 FROM Permission p WHERE p.PermissionCode = np.PermissionCode);
GO

-- Administrator always has full access, including to anything just added.
INSERT INTO RolePermission (RoleID, PermissionID, Allowed)
SELECT r.RoleID, p.PermissionID, 1
FROM Role r
CROSS JOIN Permission p
WHERE r.Code = 'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM RolePermission rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);
GO
