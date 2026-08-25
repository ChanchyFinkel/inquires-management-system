-- DB-first: this script is the source of truth for the schema. The EF Core entities in
-- Inquires.Data/Entities and the Fluent configuration in InquiresDbContext.OnModelCreating
-- are written to match what's defined here, not the other way around.
IF OBJECT_ID(N'dbo.Inquiries', N'U') IS NULL
BEGIN
    -- Statuses/Priorities are lookup tables (not a CHECK constraint or a C# enum column) so a
    -- new value can be added with an INSERT, referential integrity is enforced via FK, and
    -- each value has room for future metadata (display order, color, etc.).
    CREATE TABLE dbo.Statuses
    (
        StatusId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Statuses PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
            CONSTRAINT UQ_Statuses_Name UNIQUE
    );

    CREATE TABLE dbo.Priorities
    (
        PriorityId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Priorities PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
            CONSTRAINT UQ_Priorities_Name UNIQUE
    );

    CREATE TABLE dbo.Inquiries
    (
        InquiryId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Inquiries PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        OrganizationName NVARCHAR(200) NOT NULL,
        StatusId INT NOT NULL,
        PriorityId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Inquiries_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Inquiries_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Inquiries_Status FOREIGN KEY (StatusId)
            REFERENCES dbo.Statuses(StatusId),
        CONSTRAINT FK_Inquiries_Priority FOREIGN KEY (PriorityId)
            REFERENCES dbo.Priorities(PriorityId)
    );

    -- One index per filterable/sortable column used by InquiryRepository.GetFilteredAsync.
    -- SQL Server can intersect single-column indexes for combined filters; a composite index
    -- tailored to the most common filter+sort combination would be a targeted follow-up
    -- optimization if profiling shows it's needed, at the cost of extra index maintenance on
    -- writes. Note: IX_Inquiries_OrganizationName speeds up equality/prefix lookups and
    -- ORDER BY, but the repository's "contains" search (LIKE '%term%') cannot seek this
    -- index (leading wildcard) - a Full-Text index would be the real fix if that search needs
    -- to stay fast well beyond 10K rows.
    CREATE INDEX IX_Inquiries_StatusId
        ON dbo.Inquiries(StatusId);
    CREATE INDEX IX_Inquiries_PriorityId
        ON dbo.Inquiries(PriorityId);
    CREATE INDEX IX_Inquiries_CreatedAt
        ON dbo.Inquiries(CreatedAt DESC);
    CREATE INDEX IX_Inquiries_OrganizationName
        ON dbo.Inquiries(OrganizationName);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Name = N'New')
    INSERT INTO dbo.Statuses (Name) VALUES (N'New');
IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Name = N'InProgress')
    INSERT INTO dbo.Statuses (Name) VALUES (N'InProgress');
IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Name = N'Waiting')
    INSERT INTO dbo.Statuses (Name) VALUES (N'Waiting');
IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Name = N'Completed')
    INSERT INTO dbo.Statuses (Name) VALUES (N'Completed');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Priorities WHERE Name = N'Low')
    INSERT INTO dbo.Priorities (Name) VALUES (N'Low');
IF NOT EXISTS (SELECT 1 FROM dbo.Priorities WHERE Name = N'Medium')
    INSERT INTO dbo.Priorities (Name) VALUES (N'Medium');
IF NOT EXISTS (SELECT 1 FROM dbo.Priorities WHERE Name = N'High')
    INSERT INTO dbo.Priorities (Name) VALUES (N'High');
GO
