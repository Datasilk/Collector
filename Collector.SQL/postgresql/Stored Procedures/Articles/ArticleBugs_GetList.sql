CREATE OR REPLACE PROCEDURE  public."ArticleBugs_GetList"
(
    IN articleId INT DEFAULT 0,
    IN start INT DEFAULT 1,
    IN length INT DEFAULT 50,
    IN orderby INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM (
		SELECT ROW_NUMBER() OVER(ORDER BY 
		CASE WHEN orderby = 1 THEN "status" END ASC,
		CASE WHEN orderby = 2 THEN "status" END DESC,
		CASE WHEN orderby = 3 THEN datecreated END ASC,
		CASE WHEN orderby = 4 THEN datecreated END DESC
		) AS rownum, * FROM ArticleBugs 
			WHERE articleId = CASE WHEN articleId > 0 THEN articleId ELSE articleId END
	) AS tbl WHERE rownum >= start AND rownum < start + length
END;

$$;
