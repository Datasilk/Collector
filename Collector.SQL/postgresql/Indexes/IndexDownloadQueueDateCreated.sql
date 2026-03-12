CREATE INDEX IF NOT EXISTS "IndexDownloadQueueDateCreatedDesc" ON public."DownloadQueue" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDownloadQueueFeedUrlDateCreatedDesc" ON public."DownloadQueue" ("feedId", "url", "datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDownloadQueueDomainStatus" ON public."DownloadQueue" ("domainId", "status")
    INCLUDE ("feedId", "url", "datecreated");
