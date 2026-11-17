CREATE OR REPLACE FUNCTION public."Blacklist_Domain_Add"
(
    p_domain VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
BEGIN
    BEGIN
        INSERT INTO public."Blacklist_Domains" ("domain") VALUES (p_domain);
    EXCEPTION
        WHEN unique_violation THEN
            NULL;
    END;

    SELECT d."domainId" INTO v_domainId
    FROM public."Domains" d
    WHERE d."domain" = p_domain;

    IF v_domainId IS NOT NULL THEN
        PERFORM public."Domain_DeleteAllArticles"(v_domainId);
        DELETE FROM public."DownloadQueue" WHERE "domainId" = v_domainId;
        DELETE FROM public."Downloads" WHERE "domainId" = v_domainId;
        DELETE FROM public."Domains" WHERE "domainId" = v_domainId;
    END IF;

    DELETE FROM public."Whitelist_Domains" WHERE "domain" = p_domain;
END;
$$;