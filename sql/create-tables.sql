SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[values]
(
    [Guid]                  [uniqueidentifier]  NOT NULL DEFAULT NEWID(),
    [pm2]                   [float]             NOT NULL,
    [pm10]                  [float]             NOT NULL,
--  [EventProcessedUtcTime] [datetime2](7)      NULL,
--  [PartitionId]           [bigint]            NULL,
    [UtcTime]               [datetime2](7)      NOT NULL UNIQUE,
--  [IoTHub]                [nvarchar](4000)    NULL,
    [UnixTime]              [bigint]            NULL,
    [ClientId]              [varchar](255)      NOT NULL
) ON [PRIMARY]
GO

CREATE CLUSTERED INDEX [idx_UtcTime] ON [dbo].[values]
    ([UtcTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [idx_UnixTime] ON [dbo].[values]
    ([UnixTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO