CREATE TABLE IF NOT EXISTS public."LLM_HtmlParsers"
(
    "Id" VARCHAR(256) NOT NULL PRIMARY KEY,
    "Name" VARCHAR(256) NOT NULL,
    "Notes" TEXT,
    "Status" INT NOT NULL DEFAULT 0,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- First HTML parser model (active)
INSERT INTO public."LLM_HtmlParsers" ("Id", "Name", "Notes", "Status")
VALUES ('milkey/reader-lm-v2', 'ReaderLM v2', 'HTML → structured JSON article parser', 1)
ON CONFLICT ("Id") DO NOTHING;
