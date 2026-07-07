CREATE OR REPLACE FUNCTION public."Blacklist_Wildcard_Add"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."Blacklist_Wildcards" ("domain") VALUES (p_domain);
END;
$$;