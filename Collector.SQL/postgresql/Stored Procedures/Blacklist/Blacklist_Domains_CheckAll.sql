CREATE OR REPLACE FUNCTION public."Blacklist_Domains_CheckAll"
(
    p_domains TEXT
)
RETURNS TABLE("domain" VARCHAR(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b."domain"
    FROM public."Blacklist_Domains" b
    WHERE b."domain" = ANY(string_to_array(p_domains, ','));
END;
$$;