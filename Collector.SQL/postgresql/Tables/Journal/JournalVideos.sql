CREATE TABLE IF NOT EXISTS public."JournalVideos"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalVideos"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(64) NOT NULL,
    "OriginalFilename" VARCHAR(64) NULL DEFAULT '',
    "Url" VARCHAR(128) NULL DEFAULT '',
    "Downloaded" BOOLEAN NOT NULL DEFAULT FALSE,
    "Duration" INT NOT NULL DEFAULT 0,
    "Width" INT NOT NULL DEFAULT 1,
    "Height" INT NOT NULL DEFAULT 1,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Metadata" VARCHAR(128) NULL,
    "Title" VARCHAR(128) NULL DEFAULT '',
    "Description" VARCHAR(50) NULL,
    "FileSizeMb" DECIMAL(10, 2) NOT NULL DEFAULT 0
);CREATE INDEX IF NOT EXISTS "IX_JournalVideos_ModuleId" ON public."JournalVideos" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalVideos_EntryAndModuleId" ON public."JournalVideos" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalVideos_JournalEntryAndModuleId" ON public."JournalVideos" ("JournalId", "JournalEntryId", "ModuleId");
