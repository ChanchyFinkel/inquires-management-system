SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Inquiries', N'U') IS NULL
    THROW 50000, 'Run Database/Schema.sql before Database/Seed.sql.', 1;

DECLARE @NewStatusId INT = (SELECT StatusId FROM dbo.Statuses WHERE Name = N'New');
DECLARE @InProgressStatusId INT = (SELECT StatusId FROM dbo.Statuses WHERE Name = N'InProgress');
DECLARE @WaitingStatusId INT = (SELECT StatusId FROM dbo.Statuses WHERE Name = N'Waiting');
DECLARE @CompletedStatusId INT = (SELECT StatusId FROM dbo.Statuses WHERE Name = N'Completed');
DECLARE @LowPriorityId INT = (SELECT PriorityId FROM dbo.Priorities WHERE Name = N'Low');
DECLARE @MediumPriorityId INT = (SELECT PriorityId FROM dbo.Priorities WHERE Name = N'Medium');
DECLARE @HighPriorityId INT = (SELECT PriorityId FROM dbo.Priorities WHERE Name = N'High');

IF @NewStatusId IS NULL OR @InProgressStatusId IS NULL
    OR @WaitingStatusId IS NULL OR @CompletedStatusId IS NULL
    OR @LowPriorityId IS NULL OR @MediumPriorityId IS NULL OR @HighPriorityId IS NULL
    THROW 50001, 'Statuses and priorities must be seeded before inquiries.', 1;

;WITH Numbers AS
(
    SELECT TOP (10000)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Number
    FROM sys.all_objects AS firstObject
    CROSS JOIN sys.all_objects AS secondObject
),
SourceRows AS
(
    SELECT
        Number,
        DATEADD(DAY, -((Number - 1) % 365), SYSUTCDATETIME()) AS CreatedAt,
        CASE ABS(CHECKSUM(NEWID())) % 4
            WHEN 0 THEN @NewStatusId
            WHEN 1 THEN @InProgressStatusId
            WHEN 2 THEN @WaitingStatusId
            ELSE @CompletedStatusId
        END AS StatusId,
        CASE ABS(CHECKSUM(NEWID())) % 3
            WHEN 0 THEN @LowPriorityId
            WHEN 1 THEN @MediumPriorityId
            ELSE @HighPriorityId
        END AS PriorityId
    FROM Numbers
)
INSERT INTO dbo.Inquiries
(
    Title,
    OrganizationName,
    StatusId,
    PriorityId,
    CreatedAt,
    UpdatedAt
)
SELECT
    CONCAT(N'Environmental inquiry ', Number),
    CONCAT(N'Organization ', ((Number - 1) % 250) + 1),
    StatusId,
    PriorityId,
    CreatedAt,
    DATEADD(HOUR, Number % 72, CreatedAt)
FROM SourceRows;
GO
