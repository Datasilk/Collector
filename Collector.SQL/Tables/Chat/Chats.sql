CREATE TABLE IF NOT EXISTS public."Chats"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "AppUserId" UUID NOT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --0=deleted, 1=active
    "JournalEntryId" UUID NULL --journal entry this chat session is working on (research-agent sessions)
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Chats_AppUsers'
    ) THEN
        ALTER TABLE public."Chats"
            ADD CONSTRAINT "FK_Chats_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
    END IF;
END $$;

-- additive column for existing databases
ALTER TABLE public."Chats" ADD COLUMN IF NOT EXISTS "JournalEntryId" UUID NULL;

-- session-context compaction state (per-chat)
ALTER TABLE public."Chats" ADD COLUMN IF NOT EXISTS "ContextSummary" TEXT NULL;
ALTER TABLE public."Chats" ADD COLUMN IF NOT EXISTS "ContextSummarizedThrough" TIMESTAMP NULL;
ALTER TABLE public."Chats" ADD COLUMN IF NOT EXISTS "ContextTokens" INT NOT NULL DEFAULT 0;
