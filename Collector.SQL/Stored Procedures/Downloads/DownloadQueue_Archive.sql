CREATE OR REPLACE FUNCTION public."DownloadQueue_Archive"
(
    p_qid BIGINT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
BEGIN
    SELECT q."domainId" INTO v_domainId FROM public."DownloadQueue" q WHERE q."qid" = p_qid;
    UPDATE public."DownloadQueue" SET "status" = 2 WHERE "qid" = p_qid;
    UPDATE public."Domains" SET "inqueue" = "inqueue" - 1 WHERE "domainId" = v_domainId;
END;
$$;