CREATE TABLE IF NOT EXISTS public."ChatHistory"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ChatId" UUID NOT NULL,
    "Role" INT NOT NULL, --user, assistant, system
    "Content" TEXT NOT NULL,
    "Model" VARCHAR(50) NULL, --LLM model used (e.g., gpt-4, claude-3-opus, etc.)
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1 --0=deleted, 1=active
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ChatHistory_Chats'
    ) THEN
        ALTER TABLE public."ChatHistory"
            ADD CONSTRAINT "FK_ChatHistory_Chats" FOREIGN KEY ("ChatId") REFERENCES public."Chats"("Id");
    END IF;
END $$;
