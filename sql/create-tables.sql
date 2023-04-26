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
    ADD CONSTRAINT [UQ_UtcTime_ClientId] UNIQUE ([UtcTime], [ClientId])
GO

CREATE CLUSTERED INDEX [idx_UtcTime] ON [dbo].[values]
    ([UtcTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [idx_UnixTime] ON [dbo].[values]
    ([UnixTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


