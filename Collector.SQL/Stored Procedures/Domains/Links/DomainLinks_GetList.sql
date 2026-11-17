CREATE OR REPLACE FUNCTION public."DomainLinks_GetList"
(
    p_domainId INT
)
RETURNS TABLE(
    "domainId" INT, "domain" VARCHAR(64), "lang" VARCHAR(6), "parentId" INT, "hastitle" BOOLEAN,
    "paywall" BOOLEAN, "free" BOOLEAN, "https" BOOLEAN, "www" BOOLEAN, "empty" BOOLEAN,
    "deleted" BOOLEAN, "type" INT, "type2" INT, "articles" INT, "inqueue" INT, "title" VARCHAR(128),
    "company" VARCHAR(64), "description" VARCHAR(255), "datecreated" TIMESTAMPTZ, "dateupdated" TIMESTAMPTZ,
    "lastchecked" TIMESTAMPTZ, "whitelisted" INT, "blacklisted" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT d.*,
        (CASE WHEN wl."domain" IS NOT NULL THEN 1 ELSE 0 END) AS whitelisted,
        (CASE WHEN bl."domain" IS NOT NULL THEN 1 ELSE 0 END) AS blacklisted
    FROM public."DomainLinks" dl
    JOIN public."Domains" d ON d."domainId" = dl."linkId"
    LEFT JOIN public."Whitelist_Domains" wl ON wl."domain" = d."domain"
    LEFT JOIN public."Blacklist_Domains" bl ON bl."domain" = d."domain"
    WHERE dl."domainId" = p_domainId
    ORDER BY whitelisted DESC, blacklisted, d."domain" ASC;
END;
$$;