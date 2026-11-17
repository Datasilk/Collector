CREATE OR REPLACE FUNCTION public."Whitelist_Domain_Add"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    BEGIN
        INSERT INTO public."Whitelist_Domains" ("domain") VALUES (p_domain);
    EXCEPTION
        WHEN unique_violation THEN
            NULL;
    END;
END;
$$;