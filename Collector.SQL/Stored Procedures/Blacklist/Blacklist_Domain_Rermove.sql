CREATE OR REPLACE FUNCTION public."Blacklist_Domain_Remove"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."Blacklist_Domains" WHERE "domain" = p_domain;
END;
$$;