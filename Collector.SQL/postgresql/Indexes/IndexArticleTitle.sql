CREATE INDEX IF NOT EXISTS "IndexArticleTitles" ON public."Articles" ("title" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleTitlesDesc" ON public."Articles" ("title" DESC);
