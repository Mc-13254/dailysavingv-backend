-- Patch: fix existing roles whose Code was auto-generated as "ROL###" instead
-- of a semantic name (e.g. "CASHIER"). This is the root cause of permission
-- checks and the sidebar not matching a role's actual grants: everywhere in
-- the app that checks Role.Code (authorization policies, permission seeds,
-- notification routing, the sidebar's /my-modules lookup) expects a semantic
-- code, not an arbitrary sequence number.
--
-- Run this AFTER the Role Management code fix is deployed, so any role
-- created from now on already gets the correct code automatically.
--
-- REVIEW BEFORE RUNNING: this derives the new Code from each role's Libelle
-- (its display name). Check the preview SELECT first; if a Libelle isn't in
-- English/doesn't match what you expect, adjust manually.

-- Preview what will change:
SELECT RoleID, Code AS CurrentCode, Libelle,
       UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Libelle)), ' ', '_'), '-', '_'), '__', '_')) AS ProposedCode
FROM Role
WHERE Code LIKE 'ROL[0-9][0-9][0-9]'; -- only touches auto-generated codes, never a manually-set one like ADMIN
GO

-- Uncomment and run once you've reviewed the preview above:
-- UPDATE Role
-- SET Code = UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Libelle)), ' ', '_'), '-', '_'), '__', '_'))
-- WHERE Code LIKE 'ROL[0-9][0-9][0-9]';
-- GO
