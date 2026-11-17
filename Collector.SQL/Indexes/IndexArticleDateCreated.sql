CREATE INDEX IF NOT EXISTS "IndexArticleDateCreated" ON public."Articles" ("datecreated" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleDateCreatedDesc" ON public."Articles" ("datecreated" DESC);
