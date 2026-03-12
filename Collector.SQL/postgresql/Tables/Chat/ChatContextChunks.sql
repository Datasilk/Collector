CREATE TABLE IF NOT EXISTS public."ChatContextChunks"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "AppUserId" UUID NOT NULL,
    "ChatId" UUID NOT NULL,
    "Content" TEXT NOT NULL,
    "Embedding" VECTOR(768) NOT NULL,
    "Metadata" TEXT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."ChatContextChunks"
    ADD CONSTRAINT "FK_ChatContextChunks_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
ALTER TABLE public."ChatContextChunks"
    ADD CONSTRAINT "FK_ChatContextChunks_Chats" FOREIGN KEY ("ChatId") REFERENCES public."Chats"("Id");
