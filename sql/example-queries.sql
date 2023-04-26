-- All values last week:

SELECT *
FROM [dbo].[measurements]
WHERE EventEnqueuedUtcTime >= DATEADD(WEEK, -1, DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0))
  AND EventEnqueuedUtcTime < DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0);

-- All values one week ago:

SELECT *
FROM [dbo].[measurements]
WHERE EventEnqueuedUtcTime >= DATEADD(WEEK, -2, DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0))
  AND EventEnqueuedUtcTime < DATEADD(WEEK, -1, DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0));

-- All values two weeks ago:

SELECT *
FROM [dbo].[measurements]
WHERE EventEnqueuedUtcTime >= DATEADD(WEEK, -3, DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0))
  AND EventEnqueuedUtcTime < DATEADD(WEEK, -2, DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0));

-- All values between two selected dates (replace @StartDate and @EndDate with your desired date range):

DECLARE @StartDate datetime2 = '2023-01-01'; -- Replace with your start date
DECLARE @EndDate datetime2 = '2023-01-15'; -- Replace with your end date

SELECT *
FROM [dbo].[measurements]
WHERE EventEnqueuedUtcTime >= @StartDate
  AND EventEnqueuedUtcTime < @EndDate;



-- Testing the unique constraint on UtcTime and ClientId

-- Inserting sample data for Client1
INSERT INTO [dbo].[values] (pm2, pm10, UtcTime, UnixTime, ClientId)
VALUES (12.5, 20.3, '2023-04-26 10:00:00', 1651076400, 'Client1');

-- Inserting sample data for Client1 with the same UtcTime, this should be rejected
INSERT INTO [dbo].[values] (pm2, pm10, UtcTime, UnixTime, ClientId)
VALUES (13.2, 22.1, '2023-04-26 10:00:00', 1651076400, 'Client1');

-- Inserting sample data for Client2 with the same UtcTime, this should be allowed
INSERT INTO [dbo].[values] (pm2, pm10, UtcTime, UnixTime, ClientId)
VALUES (14.2, 25.1, '2023-04-26 10:00:00', 1651076400, 'Client2');


-- check for restraint
SELECT
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    c.name AS ColumnName
FROM
    sys.indexes i
        JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE
        i.object_id = OBJECT_ID('dbo.values') AND
        c.name = 'UtcTime' AND
        i.is_unique = 1;


-- remove restraint

ALTER TABLE [dbo].[values] DROP CONSTRAINT [UQ__values__4C21285EE83FCCC3];
GO