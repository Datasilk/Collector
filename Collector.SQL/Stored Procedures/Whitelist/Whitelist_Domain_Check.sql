CREATE OR REPLACE FUNCTION public."Whitelist_Domain_Check"
(
    p_domain VARCHAR(64)
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM public."Whitelist_Domains" WHERE "domain" = p_domain);
END;
$$;