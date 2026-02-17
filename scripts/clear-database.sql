-- Clear all data for InkAndRealm (SQL Server)
-- Order respects FK constraints. No schema changes.

SET NOCOUNT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.FeatureRelationships;
DELETE FROM dbo.TownStructures;
DELETE FROM dbo.FeaturePoints;
DELETE FROM dbo.Features;
IF OBJECT_ID('dbo.MapShares', 'U') IS NOT NULL
    DELETE FROM dbo.MapShares;
DELETE FROM dbo.Maps;
DELETE FROM dbo.Sessions;
DELETE FROM dbo.Users;

DBCC CHECKIDENT ('dbo.FeatureRelationships', RESEED, 0);
DBCC CHECKIDENT ('dbo.TownStructures', RESEED, 0);
DBCC CHECKIDENT ('dbo.FeaturePoints', RESEED, 0);
DBCC CHECKIDENT ('dbo.Features', RESEED, 0);
IF OBJECT_ID('dbo.MapShares', 'U') IS NOT NULL
    DBCC CHECKIDENT ('dbo.MapShares', RESEED, 0);
DBCC CHECKIDENT ('dbo.Maps', RESEED, 0);
DBCC CHECKIDENT ('dbo.Sessions', RESEED, 0);
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);

COMMIT TRANSACTION;
