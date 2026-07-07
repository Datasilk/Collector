CREATE OR REPLACE FUNCTION public."DownloadQueue_Move"
(
    p_qid BIGINT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."Downloads" ("id", "feedId", "domainId", "type", "status", "tries", "url", "path", "datecreated")
    SELECT q."qid", q."feedId", q."domainId", q."type", q."status", q."tries", q."url", q."path", q."datecreated"
    FROM public."DownloadQueue" q
    WHERE q."qid" = p_qid;

    DELETE FROM public."DownloadQueue" WHERE "qid" = p_qid;
END;
$$;