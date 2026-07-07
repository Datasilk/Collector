CREATE OR REPLACE FUNCTION public."Domain_GetById"
(
    p_domainId INT
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
        (SELECT COUNT(*) FROM public."Articles" a WHERE a."domainId" = p_domainId) AS totalArticles,
        CASE WHEN EXISTS(SELECT 1 FROM public."Whitelist_Domains" wl WHERE wl."domain" = d."domain") THEN 1 ELSE 0 END AS whitelisted,
        CASE WHEN EXISTS(SELECT 1 FROM public."Blacklist_Domains" bl WHERE bl."domain" = d."domain") THEN 1 ELSE 0 END AS blacklisted
    FROM public."Domains" d
    WHERE d."domainId" = p_domainId;
END;
$$;