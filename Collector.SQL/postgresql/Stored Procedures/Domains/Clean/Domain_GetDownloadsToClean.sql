CREATE OR REPLACE FUNCTION public."Domain_GetDownloadsToClean"
(
    p_domainId INT,
    p_topten BOOLEAN DEFAULT FALSE
)
RETURNS TABLE("articleId" INT, "title" VARCHAR(250), "url" VARCHAR(250))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT a."articleId", a."title", a."url"
    FROM public."Articles" a
    JOIN public."DownloadRules" r ON r."domainId" = a."domainId"
    WHERE a."domainId" = p_domainId
    AND (
        (LENGTH(r."url") > 0 AND a."url" ILIKE '%' || r."url" || '%')
        OR (LENGTH(r."title") > 0 AND a."title" ILIKE '%' || r."title" || '%')
        OR (LENGTH(r."summary") > 0 AND a."summary" ILIKE '%' || r."summary" || '%')
    )
    LIMIT CASE WHEN p_topten THEN 10 ELSE NULL END;
END;
$$;