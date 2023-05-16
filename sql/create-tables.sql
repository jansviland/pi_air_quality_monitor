SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[values]
(
    [Guid]     [uniqueidentifier] NOT NULL DEFAULT NEWID(),
    [pm2]      [float]            NOT NULL,
    [pm10]     [float]            NOT NULL,
    [UtcTime]  [datetime2](7)     NOT NULL, -- The same client can not send the same UtcTime twice, all measurements should have a unique UtcTime
    [UnixTime] [bigint]           NULL,
    [ClientId] [varchar](255)     NOT NULL
) ON [PRIMARY]
GO

-- UNIQUE constraint (UQ_UtcTime_ClientId) on the combination of UtcTime and ClientId columns. 
-- This way, you can have multiple rows with the same UtcTime as long as the ClientId is different.
ALTER TABLE [dbo].[values]
    ADD CONSTRAINT [UQ_UtcTime_ClientId_Constraint] UNIQUE ([UtcTime], [ClientId])
GO

-- The idx_ClientId_UtcTime index is a combination of ClientId ascending and UtcTime descending, this means that the data is physically stored in that order. 
-- A query that looks for a specific ClientId and a range of UtcTime values will be able to use the index to quickly find the data.
--
-- For example: 
-- SELECT * FROM [dbo].[values] WHERE ClientId = '123' AND UtcTime BETWEEN '2020-01-01' AND '2020-01-02'
-- will be able to use the index to quickly find the data.
CREATE CLUSTERED INDEX [idx_ClientId_UtcTime] ON [dbo].[values]
    ([ClientId] ASC, [UtcTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [idx_UnixTime] ON [dbo].[values]
    ([UnixTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


