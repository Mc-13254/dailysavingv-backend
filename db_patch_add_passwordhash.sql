-- Patch: add PasswordHash to UsersTmp so pending user-creation requests can
-- carry the hashed password until a Supervisor/Admin approves them.
ALTER TABLE UsersTmp ADD PasswordHash NVARCHAR(300) NULL;
GO
