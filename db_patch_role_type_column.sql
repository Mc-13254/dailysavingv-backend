-- Patch: separates "Code" (sequential ID, ROL001/ROL002...) from "RoleType"
-- (the semantic classification security actually relies on). This replaces
-- the earlier, incorrect approach of deriving Code from the role name.
--
-- After this: Code stays a pure sequential number (driven by Numbering
-- Parameters, as intended), and RoleType is the explicit ADMIN/SUPERVISOR/
-- MANAGER/CASHIER/COLLECTOR/CUSTOM classification used by every
-- authorization check, notification routing, and default permission grant.
--
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Role', 'RoleType') IS NULL
    ALTER TABLE Role ADD RoleType NVARCHAR(20) NOT NULL DEFAULT 'CUSTOM';
GO

-- Best-effort classification from existing Code/Libelle. Review the result
-- afterwards (SELECT RoleID, Code, Libelle, RoleType FROM Role) and correct
-- any role manually via Gestion des Rôles if the automatic guess is wrong —
-- from now on RoleType is set explicitly through a dropdown at creation, not
-- guessed.
UPDATE Role SET RoleType = 'ADMIN'      WHERE Code = 'ADMIN' OR Libelle LIKE '%Admin%';
UPDATE Role SET RoleType = 'SUPERVISOR' WHERE RoleType = 'CUSTOM' AND (Code = 'SUPERVISOR' OR Libelle LIKE '%Supervis%');
UPDATE Role SET RoleType = 'MANAGER'    WHERE RoleType = 'CUSTOM' AND (Libelle LIKE '%Manager%' OR Libelle LIKE '%G[ée]rant%');
UPDATE Role SET RoleType = 'CASHIER'    WHERE RoleType = 'CUSTOM' AND (Libelle LIKE '%Cash%' OR Libelle LIKE '%Caiss%');
UPDATE Role SET RoleType = 'COLLECTOR'  WHERE RoleType = 'CUSTOM' AND (Code = 'COLLECTOR' OR Libelle LIKE '%Collect%');
GO

-- Reset Code back to a clean sequential ROL### for every role whose Code
-- isn't already the plain sequential format (i.e. anything touched by the
-- earlier semantic-slug migration). Ordered by RoleID to stay stable.
;WITH Numbered AS (
    SELECT RoleID, Code, ROW_NUMBER() OVER (ORDER BY RoleID) AS rn
    FROM Role
    WHERE Code NOT LIKE 'ROL[0-9][0-9][0-9]'
)
UPDATE r
SET r.Code = 'ROL' + RIGHT('000' + CAST(n.rn AS VARCHAR(10)), 3)
FROM Role r
JOIN Numbered n ON n.RoleID = r.RoleID;
GO

SELECT RoleID, Code, Libelle, RoleType FROM Role ORDER BY RoleID;
