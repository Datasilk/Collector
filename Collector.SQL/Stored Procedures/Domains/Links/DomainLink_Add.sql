CREATE OR REPLACE FUNCTION public."DomainLink_Add"
(
    p_domainId INT,
    p_linkId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    BEGIN
        INSERT INTO public."DomainLinks" ("domainId", "linkId") VALUES (p_domainId, p_linkId);
    EXCEPTION
        WHEN unique_violation THEN
            NULL;
    END;
END;
$$;