CREATE TABLE IF NOT EXISTS public."DomainCollections"
(
    "colId" INT NOT NULL PRIMARY KEY,
    "colgroupId" INT NULL, -- collection group ID
    "name" VARCHAR(32) NOT NULL,
    "search" VARCHAR(128) NOT NULL DEFAULT '',
    "subjectId" INT NOT NULL DEFAULT 0,
    "filtertype" INT NOT NULL DEFAULT 0,
    "type" INT NOT NULL DEFAULT -1,
    "sort" INT NOT NULL DEFAULT 0,
    "lang" VARCHAR(6) NOT NULL DEFAULT '',
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionDates" ON public."DomainCollections" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionNames" ON public."DomainCollections" ("name" DESC);
