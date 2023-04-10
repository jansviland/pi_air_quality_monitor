SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[measurements]
(
    [pm2]                   [float]          NOT NULL,
    [pm10]                  [float]          NOT NULL,
    [EventProcessedUtcTime] [datetime2](7)   NULL,
    [PartitionId]           [bigint]         NULL,
    [EventEnqueuedUtcTime]  [datetime2](7)   NOT NULL UNIQUE,
    [IoTHub]                [nvarchar](4000) NULL,
    [unixtime]              [bigint]         NULL,
    [client_id]             [varchar](255)   NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [idx_unixtime] ON [dbo].[measurements]
    ([unixtime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE CLUSTERED INDEX [idx_EventEnqueuedUtcTime] ON [dbo].[measurements]
    ([EventEnqueuedUtcTime] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO