CREATE OR REPLACE FUNCTION public."Domain_DownloadRules_GetForDomains"
(
    p_domains TEXT
)
RETURNS TABLE(
    "ruleId" INT, "domainId" INT, "rule" BOOLEAN, "url" VARCHAR(64), "title" VARCHAR(64),
    "summary" VARCHAR(64), "datecreated" TIMESTAMPTZ, "domain" VARCHAR(64)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainNames TEXT[] := string_to_array(p_domains, ',');
BEGIN
    RETURN QUERY
    SELECT r.*, d."domain"
    FROM public."Domains" d
    JOIN public."DownloadRules" r ON r."domainId" = d."domainId"
    WHERE d."domain" = ANY(v_domainNames)
    ORDER BY d."domainId" ASC;
END;
$$;