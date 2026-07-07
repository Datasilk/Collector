CREATE OR REPLACE FUNCTION public."Feeds_Check"
(
    p_feedId INT DEFAULT 0
)
RETURNS TABLE(
    "feedId" INT, "domainId" INT, "doctype" INT, "categoryId" INT, "title" VARCHAR(100), "url" VARCHAR(100),
    "checkIntervals" INT, "lastChecked" TIMESTAMP, "filter" TEXT, "category" VARCHAR(64)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT f.*, c."title" AS "category"
    FROM public."Feeds" f
    JOIN public."FeedCategories" c ON c."categoryId" = f."categoryId"
    WHERE f."lastChecked" < CURRENT_TIMESTAMP - (f."checkIntervals" || ' minutes')::INTERVAL
    AND (
        (p_feedId > 0 AND f."feedId" = p_feedId)
        OR p_feedId = 0
    );
END;
$$;