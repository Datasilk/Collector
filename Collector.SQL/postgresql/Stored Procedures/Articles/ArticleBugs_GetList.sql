CREATE OR REPLACE FUNCTION public."ArticleBugs_GetList"
(
    p_articleId INT DEFAULT 0,
    p_start INT DEFAULT 1,
    p_length INT DEFAULT 50,
    p_orderby INT DEFAULT 1
)
RETURNS TABLE("rownum" BIGINT, "bugId" INT, "articleId" INT, "title" VARCHAR(100), "description" TEXT, "datecreated" TIMESTAMP, "status" SMALLINT)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT ROW_NUMBER() OVER(ORDER BY
            CASE WHEN p_orderby = 1 THEN b."status" END ASC,
            CASE WHEN p_orderby = 2 THEN b."status" END DESC,
            CASE WHEN p_orderby = 3 THEN b."datecreated" END ASC,
            CASE WHEN p_orderby = 4 THEN b."datecreated" END DESC
        ) AS rownum, b.* FROM public."ArticleBugs" b
        WHERE b."articleId" = CASE WHEN p_articleId > 0 THEN p_articleId ELSE b."articleId" END
    ) AS tbl WHERE tbl.rownum >= p_start AND tbl.rownum < p_start + p_length;
END;
$$;