-- Database initialization script for NotificationService
-- This script runs when the SQL Server container starts

USE master;
GO

-- Create the NotificationDb database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NotificationDb')
BEGIN
    CREATE DATABASE NotificationDb;
    PRINT 'Database NotificationDb created successfully';
END
ELSE
BEGIN
    PRINT 'Database NotificationDb already exists';
END
GO

USE NotificationDb;
GO

-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dbo')
BEGIN
    EXEC('CREATE SCHEMA dbo');
END
GO

-- Create Notifications table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    CREATE TABLE Notifications (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Type INT NOT NULL,
        ToAddress NVARCHAR(256) NOT NULL,
        Subject NVARCHAR(256) NULL,
        Body NVARCHAR(MAX) NOT NULL,
        Metadata NVARCHAR(MAX) NULL,
        Status INT NOT NULL DEFAULT 0,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        SentAt DATETIMEOFFSET NULL,
        Error NVARCHAR(1024) NULL
    );

    CREATE INDEX IX_Notifications_Type ON Notifications(Type);
    CREATE INDEX IX_Notifications_Status ON Notifications(Status);
    CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);

    PRINT 'Notifications table created successfully';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists';
END
GO

-- Create a login and user for development (remove in production)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'notificationservice')
BEGIN
    CREATE LOGIN notificationservice WITH PASSWORD = 'DevPassword123!';
    PRINT 'Development login created';
END
GO

USE NotificationDb;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'notificationservice')
BEGIN
    CREATE USER notificationservice FOR LOGIN notificationservice;
    ALTER ROLE db_owner ADD MEMBER notificationservice;
    PRINT 'Development user created';
END
GO

PRINT 'Database initialization completed successfully';
