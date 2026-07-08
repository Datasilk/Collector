DROP FUNCTION IF EXISTS public."Domains_GetNextUnanalyzed";

CREATE FUNCTION public."Domains_GetNextUnanalyzed"()
RETURNS TABLE(
    "domainId" INT, "domain" VARCHAR(64), "lang" VARCHAR(6), "parentId" INT,
    "hastitle" BOOLEAN, "paywall" BOOLEAN, "free" BOOLEAN, "https" BOOLEAN, "www" BOOLEAN,
    "empty" BOOLEAN, "deleted" BOOLEAN, "type" INT, "type2" INT, "articles" INT, "inqueue" INT,
    "title" VARCHAR(128), "company" VARCHAR(64), "description" VARCHAR(255), "datecreated" TIMESTAMPTZ,
    "dateupdated" TIMESTAMPTZ, "lastchecked" TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT d."domainId", d."domain", d."lang", d."parentId",
           d."hastitle", d."paywall", d."free", d."https", d."www",
           d."empty", d."deleted", d."type", d."type2", d."articles", d."inqueue",
           d."title", d."company", d."description", d."datecreated",
           d."dateupdated", d."lastchecked"
    FROM public."Domains" d
    WHERE d."deleted" = FALSE
      AND d."empty" = FALSE
      AND (d."type" = -1 OR d."type" = 0)
      AND (d."type2" = -1 OR d."type2" = 0)
    ORDER BY d."datecreated" ASC
    LIMIT 1;
END;
$$;
