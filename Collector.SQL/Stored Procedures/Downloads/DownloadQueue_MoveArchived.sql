CREATE OR REPLACE FUNCTION public."DownloadQueue_MoveArchived"()
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."Downloads" ("id", "feedId", "domainId", "type", "status", "tries", "url", "path", "datecreated")
    SELECT q."qid", q."feedId", q."domainId", q."type", q."status", q."tries", q."url", q."path", q."datecreated"
    FROM public."DownloadQueue" q
    WHERE q."status" = 2
    AND NOT EXISTS(SELECT 1 FROM public."Downloads" d WHERE d."id" = q."qid");

    DELETE FROM public."DownloadQueue" WHERE "status" = 2;
END;
$$;