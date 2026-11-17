CREATE OR REPLACE FUNCTION public."Download_Delete"
(
    p_qid BIGINT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_url VARCHAR(250);
    v_domainId INT;
BEGIN
    SELECT q."url", q."domainId" INTO v_url, v_domainId
    FROM public."DownloadQueue" q
    WHERE q."qid" = p_qid;

    DELETE FROM public."Articles" WHERE "url" = v_url;
    DELETE FROM public."DownloadQueue" WHERE "qid" = p_qid;
    DELETE FROM public."Downloads" WHERE "id" = p_qid;
    UPDATE public."Domains" SET "inqueue" = "inqueue" - 1 WHERE "domainId" = v_domainId;
END;
$$;