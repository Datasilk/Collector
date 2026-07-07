CREATE OR REPLACE FUNCTION public."Domain_GetInfo"
(
    p_domain VARCHAR(64)
)
RETURNS TABLE(
    "domainId" INT, "domain" VARCHAR(64), "lang" VARCHAR(6), "parentId" INT, "hastitle" BOOLEAN,
    "paywall" BOOLEAN, "free" BOOLEAN, "https" BOOLEAN, "www" BOOLEAN, "empty" BOOLEAN,
    "deleted" BOOLEAN, "type" INT, "type2" INT, "articles" INT, "inqueue" INT, "title" VARCHAR(128),
    "company" VARCHAR(64), "description" VARCHAR(255), "datecreated" TIMESTAMPTZ, "dateupdated" TIMESTAMPTZ,
    "lastchecked" TIMESTAMPTZ, "totalArticles" INT, "whitelisted" INT, "blacklisted" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT d.*,
        (SELECT COUNT(*) FROM public."Articles" a WHERE a."domainId" = d."domainId") AS totalArticles,
        CASE WHEN EXISTS(SELECT 1 FROM public."Whitelist_Domains" wl WHERE wl."domain" = p_domain) THEN 1 ELSE 0 END AS whitelisted,
        CASE WHEN EXISTS(SELECT 1 FROM public."Blacklist_Domains" bl WHERE bl."domain" = d."domain") THEN 1 ELSE 0 END AS blacklisted
    FROM public."Domains" d
    WHERE d."domain" = p_domain;
END;
$$;