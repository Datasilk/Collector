CREATE OR REPLACE FUNCTION public."Feeds_Filter"
(
    p_start INT,
    p_length INT,
    p_search VARCHAR(255),
    p_sort INT
)
RETURNS TABLE(
    "feedId" INT, "domainId" INT, "doctype" INT, "categoryId" INT, "title" VARCHAR(100), "url" VARCHAR(100),
    "checkIntervals" INT, "lastChecked" TIMESTAMP, "filter" TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT f.*
    FROM public."Feeds" f
    WHERE f."title" ILIKE '%' || p_search || '%' OR f."url" ILIKE '%' || p_search || '%'
    ORDER BY
        CASE WHEN p_sort = 0 THEN f."title" END ASC,
        CASE WHEN p_sort = 1 THEN f."title" END DESC,
        CASE WHEN p_sort = 2 THEN f."url" END ASC,
        CASE WHEN p_sort = 3 THEN f."url" END DESC,
        CASE WHEN p_sort = 4 THEN f."checkIntervals" END ASC,
        CASE WHEN p_sort = 5 THEN f."checkIntervals" END DESC
    OFFSET p_start ROWS FETCH NEXT p_length ROWS ONLY;
END;
$$;