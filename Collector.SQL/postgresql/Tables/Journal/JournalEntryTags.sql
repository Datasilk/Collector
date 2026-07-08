CREATE TABLE IF NOT EXISTS public."JournalEntryTags"
(
    "TagId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'public."JournalEntryTags"'::regclass AND contype = 'p'
    ) THEN
        ALTER TABLE public."JournalEntryTags"
            ADD CONSTRAINT "PK_JournalEntryTags" PRIMARY KEY ("TagId", "JournalEntryId");
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryId" ON public."JournalEntryTags" ("JournalEntryId");

CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryAndTag" ON public."JournalEntryTags" ("JournalEntryId", "TagId");
