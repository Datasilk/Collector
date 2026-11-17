CREATE OR REPLACE FUNCTION public."Blacklist_Wildcard_Remove"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."Blacklist_Wildcards" WHERE "domain" = p_domain;
END;
$$;