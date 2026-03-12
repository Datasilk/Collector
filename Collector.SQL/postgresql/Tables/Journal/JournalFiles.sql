CREATE TABLE IF NOT EXISTS public."JournalFiles"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalFiles"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(256) NOT NULL,
    "FileSize" BIGINT NOT NULL DEFAULT 0,
    "DateUploaded" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);CREATE INDEX IF NOT EXISTS "IX_JournalFiles_ModuleId" ON public."JournalFiles" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalFiles_EntryAndModuleId" ON public."JournalFiles" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalFiles_JournalEntryAndModuleId" ON public."JournalFiles" ("JournalId", "JournalEntryId", "ModuleId");
