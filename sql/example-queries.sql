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