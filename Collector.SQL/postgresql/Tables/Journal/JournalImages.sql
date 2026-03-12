CREATE TABLE IF NOT EXISTS public."JournalImages"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalImages"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(64) NOT NULL,
    "Width" INT NOT NULL DEFAULT 1,
    "Height" INT NOT NULL DEFAULT 1
);CREATE INDEX IF NOT EXISTS "IX_JournalImages_ModuleId" ON public."JournalImages" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalImages_EntryAndModuleId" ON public."JournalImages" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalImages_JournalEntryAndModuleId" ON public."JournalImages" ("JournalId", "JournalEntryId", "ModuleId");
