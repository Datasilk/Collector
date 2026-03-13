CREATE TABLE IF NOT EXISTS public."OllamaModels"
(
    "Id" VARCHAR(128) NOT NULL PRIMARY KEY,
    "Name" VARCHAR(128) NOT NULL,
    "Notes" TEXT,
    "Status" INT NOT NULL DEFAULT 0, --0=inactive, 1=active
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create index for status to quickly find active model
CREATE INDEX IF NOT EXISTS "IX_OllamaModels_Status" ON public."OllamaModels" ("Status");
