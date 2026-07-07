CREATE OR REPLACE FUNCTION public."Domain_Delete"
(
    p_domainId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_domain VARCHAR(128);
BEGIN
    SELECT d."domain" INTO v_domain FROM public."Domains" d WHERE d."domainId" = p_domainId;

    PERFORM public."Domain_DeleteAllArticles"(p_domainId);
    DELETE FROM public."Domains" WHERE "domainId" = p_domainId;
    DELETE FROM public."DownloadQueue" WHERE "domainId" = p_domainId;
    DELETE FROM public."Downloads" WHERE "domainId" = p_domainId;
    DELETE FROM public."Whitelist_Domains" WHERE "domain" = v_domain;
    DELETE FROM public."Blacklist_Domains" WHERE "domain" = v_domain;
END;
$$;