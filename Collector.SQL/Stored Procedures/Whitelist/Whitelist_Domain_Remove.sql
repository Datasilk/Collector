CREATE OR REPLACE FUNCTION public."Whitelist_Domain_Remove"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."Whitelist_Domains" WHERE "domain" = p_domain;
END;
$$;