CREATE TABLE IF NOT EXISTS public."Feeds"
(
    "feedId" INT NOT NULL PRIMARY KEY,
    "domainId" INT NOT NULL DEFAULT 0,
    "doctype" INT NULL, -- 1 = RSS, 2 = HTML
    "categoryId" INT NULL,
    "title" VARCHAR(100) NULL,
    "url" VARCHAR(100) NULL,
    "checkIntervals" INT NULL,
    "lastChecked" TIMESTAMP NULL,
    "filter" TEXT NULL
);
