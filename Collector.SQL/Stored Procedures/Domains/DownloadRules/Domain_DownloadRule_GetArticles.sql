CREATE OR REPLACE FUNCTION public."Domain_DownloadRule_GetArticles"
(
    p_ruleId INT
)
RETURNS TABLE("articleId" INT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
    v_url VARCHAR(64);
    v_title VARCHAR(64);
    v_summary VARCHAR(64);
BEGIN
    SELECT r."domainId", r."url", r."title", r."summary"
    INTO v_domainId, v_url, v_title, v_summary
    FROM public."DownloadRules" r
    WHERE r."ruleId" = p_ruleId;

    RETURN QUERY
    SELECT a."articleId"
    FROM public."Articles" a
    WHERE a."domainId" = v_domainId
    AND (
        a."url" ILIKE '%' || v_url || '%'
        OR (LENGTH(v_title) > 0 AND a."title" ILIKE '%' || v_title || '%')
        OR (LENGTH(v_summary) > 0 AND a."summary" ILIKE '%' || v_summary || '%')
    );
END;
$$;