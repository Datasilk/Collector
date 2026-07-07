CREATE OR REPLACE FUNCTION public."Article_UpdateUrl"
(
    p_articleId INT DEFAULT 0,
    p_url VARCHAR(250),
    p_domain VARCHAR(250),
    p_parentId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_oldurl VARCHAR(250);
    v_domainId INT;
    v_newarticleId INT;
BEGIN
    SELECT a."url" INTO v_oldurl
    FROM public."Articles" a
    WHERE a."articleId" = p_articleId;

    SELECT d."domainId" INTO v_domainId
    FROM public."Domains" d
    WHERE d."domain" = p_domain;

    IF v_domainId IS NULL THEN
        PERFORM public."Domain_Add"(p_domain, p_parentId);

        SELECT d."domainId" INTO v_domainId
        FROM public."Domains" d
        WHERE d."domain" = p_domain;
    END IF;

    IF v_oldurl <> p_url THEN
        SELECT a."articleId" INTO v_newarticleId
        FROM public."Articles" a
        WHERE a."url" = p_url
        ORDER BY a."datecreated" ASC
        LIMIT 1;

        IF v_newarticleId IS NOT NULL AND v_newarticleId <> p_articleId THEN
            DELETE FROM public."Articles" WHERE "articleId" = p_articleId;
        END IF;

        UPDATE public."Articles"
        SET "url" = p_url,
            "domainId" = v_domainId,
            "domain" = p_domain
        WHERE "articleId" = p_articleId;

        DELETE FROM public."DownloadQueue" WHERE "url" = p_url;
        DELETE FROM public."Downloads" WHERE "url" = p_url;
        UPDATE public."DownloadQueue" SET "url" = p_url, "domainId" = v_domainId WHERE "url" = v_oldurl;
        UPDATE public."Downloads" SET "url" = p_url, "domainId" = v_domainId WHERE "url" = v_oldurl;
    END IF;
END;
$$;