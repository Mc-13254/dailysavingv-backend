-- Patch: the Photo column was originally sized for a short file path/URL,
-- but the UI now uploads and stores the image itself as a base64 string,
-- which is far larger and was getting truncated ("String or binary data
-- would be truncated"). Widen it to NVARCHAR(MAX) to match Signe/LogoBase64.

ALTER TABLE Users ALTER COLUMN Photo NVARCHAR(MAX) NULL;
GO

ALTER TABLE UsersTmp ALTER COLUMN Photo NVARCHAR(MAX) NULL;
GO
