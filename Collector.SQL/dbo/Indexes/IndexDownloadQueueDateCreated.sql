CREATE INDEX [IndexDownloadQueueDateCreatedDesc] ON [dbo].[DownloadQueue] ([datecreated] DESC)
GO
CREATE INDEX [IndexDownloadQueueFeedUrlDateCreatedDesc] ON [dbo].[DownloadQueue] ([feedId], [url], [datecreated] DESC)
GO
CREATE INDEX [IndexDownloadQueueDomainStatus] ON [dbo].[DownloadQueue] ([domainId], [status])
INCLUDE ([feedId], [url], [datecreated])