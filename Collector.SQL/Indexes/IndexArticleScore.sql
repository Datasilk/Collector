CREATE INDEX IF NOT EXISTS "IndexArticleScore" ON public."Articles" ("score" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleScoreDesc" ON public."Articles" ("score" DESC);
