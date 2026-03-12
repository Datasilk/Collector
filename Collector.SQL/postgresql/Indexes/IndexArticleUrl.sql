CREATE INDEX IF NOT EXISTS "IndexArticleUrl" ON public."Articles" ("url" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleUrlDesc" ON public."Articles" ("url" DESC);
