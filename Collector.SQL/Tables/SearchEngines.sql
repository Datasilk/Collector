CREATE TABLE IF NOT EXISTS public."SearchEngines"
(
    "searchEngineId" SERIAL PRIMARY KEY,
    "name" VARCHAR(128) NOT NULL,
    "url" VARCHAR(512) NOT NULL,
    "type" VARCHAR(64) NOT NULL DEFAULT 'web',
    "method" VARCHAR(8) NOT NULL DEFAULT 'GET',
    "headervars" TEXT,
    "requestbodyvars" TEXT,
    "requestbodydatatype" VARCHAR(32) NOT NULL DEFAULT 'form',
    "discoveredfrom" VARCHAR(512),
    "sort" INT NOT NULL DEFAULT 0,
    "enabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "dateupdated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Upgrade pre-existing installs
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "type" VARCHAR(64) NOT NULL DEFAULT 'web';
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "method" VARCHAR(8) NOT NULL DEFAULT 'GET';
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "headervars" TEXT;
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "requestbodyvars" TEXT;
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "requestbodydatatype" VARCHAR(32) NOT NULL DEFAULT 'form';
ALTER TABLE public."SearchEngines" ADD COLUMN IF NOT EXISTS "discoveredfrom" VARCHAR(512);

-- Seed default engines when the table is empty so existing research behavior is preserved
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public."SearchEngines") THEN
        INSERT INTO public."SearchEngines" ("name", "url", "type", "sort", "enabled") VALUES
            ('Google', 'https://www.google.com/search?q={search}&num=20', 'web', 1, TRUE),
            ('Bing', 'https://www.bing.com/search?q={search}&count=20', 'web', 2, TRUE),
            ('DuckDuckGo', 'https://duckduckgo.com/html/?q={search}', 'web', 3, TRUE);
    END IF;
END $$;
