CREATE TABLE IF NOT EXISTS public."JournalEntryTags"
(
    "TagId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL
);
ALTER TABLE public."JournalEntryTags"
    ADD CONSTRAINT "PK_JournalEntryTags" PRIMARY KEY ("TagId", "JournalEntryId");CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryId" ON public."JournalEntryTags" ("JournalEntryId");

CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryAndTag" ON public."JournalEntryTags" ("JournalEntryId", "TagId");
