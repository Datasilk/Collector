CREATE OR REPLACE FUNCTION public."Domain_DownloadRules_GetList"
(
    p_domainId INT
)
RETURNS TABLE("ruleId" INT, "domainId" INT, "rule" BOOLEAN, "url" VARCHAR(64), "title" VARCHAR(64), "summary" VARCHAR(64), "datecreated" TIMESTAMPTZ)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."DownloadRules" r WHERE r."domainId" = p_domainId ORDER BY r."datecreated" ASC;
END;
$$;