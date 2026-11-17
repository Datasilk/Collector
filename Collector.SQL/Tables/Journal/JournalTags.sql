CREATE TABLE IF NOT EXISTS public."JournalTags"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalTags"')),
    "JournalId" INT NOT NULL,
    "Tag" VARCHAR(32) NOT NULL
);CREATE INDEX IF NOT EXISTS "IX_JournalTags_JournalId" ON public."JournalTags" ("JournalId");

CREATE INDEX IF NOT EXISTS "IX_JournalTags_JournalAndTag" ON public."JournalTags" ("JournalId", "Tag");
