CREATE OR REPLACE FUNCTION public."Domain_DownloadRule_GetDownloads"
(
    p_ruleId INT
)
RETURNS TABLE("qid" BIGINT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
    v_url VARCHAR(64);
BEGIN
    SELECT r."domainId", r."url"
    INTO v_domainId, v_url
    FROM public."DownloadRules" r
    WHERE r."ruleId" = p_ruleId;

    RETURN QUERY
    SELECT dq."qid"
    FROM public."DownloadQueue" dq
    WHERE dq."domainId" = v_domainId
    AND dq."url" ILIKE '%' || v_url || '%';
END;
$$;