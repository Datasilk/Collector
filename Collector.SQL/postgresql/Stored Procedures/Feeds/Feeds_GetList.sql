CREATE OR REPLACE FUNCTION public."Feeds_GetList"()
RETURNS TABLE(
    "feedId" INT, "domainId" INT, "doctype" INT, "categoryId" INT, "title" VARCHAR(100), "url" VARCHAR(100),
    "checkIntervals" INT, "lastChecked" TIMESTAMP, "filter" TEXT, "category" VARCHAR(64)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT f.*, fc."title" AS "category"
    FROM public."Feeds" f
    JOIN public."FeedCategories" fc ON fc."categoryId" = f."categoryId"
    WHERE f."feedId" > 0 ORDER BY fc."title" ASC, f."title" ASC;
END;
$$;