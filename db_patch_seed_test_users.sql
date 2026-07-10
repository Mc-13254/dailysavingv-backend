-- Patch: one test user per role (CASHIER, SUPERVISOR, MANAGER, COLLECTOR),
-- to make testing role-based access/permissions straightforward.
-- Default password for ALL of them: Test@1234 (must be changed at first login).
-- IDEMPOTENT: safe to re-run — skips any role/user that already exists.

DECLARE @AgenceID INT = (SELECT TOP 1 AgenceID FROM Agence ORDER BY AgenceID);
DECLARE @PasswordHash NVARCHAR(200) = '$2b$10$Wv1K76sJmmV8M1932PQZZurHD7BR/Bu.Nb1iZIpFuxgIGUBjQ0XUC'; -- Test@1234

IF @AgenceID IS NULL
BEGIN
    PRINT 'No agency found — cannot seed test users. Create at least one Agency first.';
END
ELSE
BEGIN
    -- CASHIER
    IF EXISTS (SELECT 1 FROM Role WHERE Code = 'CASHIER') AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'test.cashier')
        INSERT INTO Users (CodeUser, Username, PasswordHash, RoleID, FirstName, LastName, AgenceID, Statut, ValidationStatus, MustChangePassword)
        SELECT 'U-TEST-CASH', 'test.cashier', @PasswordHash, RoleID, 'Test', 'Cashier', @AgenceID, 'ACTIVE', 'VALIDATED', 1
        FROM Role WHERE Code = 'CASHIER';

    -- SUPERVISOR
    IF EXISTS (SELECT 1 FROM Role WHERE Code = 'SUPERVISOR') AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'test.supervisor')
        INSERT INTO Users (CodeUser, Username, PasswordHash, RoleID, FirstName, LastName, AgenceID, Statut, ValidationStatus, MustChangePassword)
        SELECT 'U-TEST-SUP', 'test.supervisor', @PasswordHash, RoleID, 'Test', 'Supervisor', @AgenceID, 'ACTIVE', 'VALIDATED', 1
        FROM Role WHERE Code = 'SUPERVISOR';

    -- MANAGER
    IF EXISTS (SELECT 1 FROM Role WHERE Code = 'MANAGER') AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'test.manager')
        INSERT INTO Users (CodeUser, Username, PasswordHash, RoleID, FirstName, LastName, AgenceID, Statut, ValidationStatus, MustChangePassword)
        SELECT 'U-TEST-MGR', 'test.manager', @PasswordHash, RoleID, 'Test', 'Manager', @AgenceID, 'ACTIVE', 'VALIDATED', 1
        FROM Role WHERE Code = 'MANAGER';

    -- COLLECTOR (user account only — not the Collector business-entity profile)
    IF EXISTS (SELECT 1 FROM Role WHERE Code = 'COLLECTOR') AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'test.collector')
        INSERT INTO Users (CodeUser, Username, PasswordHash, RoleID, FirstName, LastName, AgenceID, Statut, ValidationStatus, MustChangePassword)
        SELECT 'U-TEST-COL', 'test.collector', @PasswordHash, RoleID, 'Test', 'Collector', @AgenceID, 'ACTIVE', 'VALIDATED', 1
        FROM Role WHERE Code = 'COLLECTOR';

    -- ADMIN (only created if none exists yet — most installs already have one)
    IF EXISTS (SELECT 1 FROM Role WHERE Code = 'ADMIN') AND NOT EXISTS (SELECT 1 FROM Users u INNER JOIN Role r ON u.RoleID = r.RoleID WHERE r.Code = 'ADMIN')
        INSERT INTO Users (CodeUser, Username, PasswordHash, RoleID, FirstName, LastName, AgenceID, Statut, ValidationStatus, MustChangePassword)
        SELECT 'U-TEST-ADM', 'test.admin', @PasswordHash, RoleID, 'Test', 'Admin', @AgenceID, 'ACTIVE', 'VALIDATED', 1
        FROM Role WHERE Code = 'ADMIN';
END
GO
