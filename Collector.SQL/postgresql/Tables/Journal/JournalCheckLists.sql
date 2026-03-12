CREATE TABLE IF NOT EXISTS public."JournalCheckLists"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCheckLists"')),
    "AppUserId" UUID NOT NULL,
    "EntryId" UUID NOT NULL,
    "ThemeId" INT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Description" VARCHAR(512) NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1
);
