CREATE OR REPLACE FUNCTION public."DownloadQueue_Check_DownloadRules"
(
    p_qid BIGINT
)
RETURNS TABLE(
    "ruleId" INT, "domainId" INT, "rule" BOOLEAN, "domain" VARCHAR(64), "url" VARCHAR(64), "title" VARCHAR(64), "summary" VARCHAR(64), "datecreated" TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT r."ruleId", r."domainId", r."rule", d."domain", r."url", r."title", r."summary", r."datecreated"
    FROM public."DownloadRules" r
    JOIN public."DownloadQueue" q ON q."domainId" = r."domainId"
    JOIN public."Domains" d ON d."domainId" = r."domainId"
    WHERE q."qid" = p_qid;
END;
$$;
