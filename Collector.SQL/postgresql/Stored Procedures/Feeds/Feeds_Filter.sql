CREATE OR REPLACE PROCEDURE  public."Feeds_Filter"
(
    IN Start INT,
    IN Length INT,
    IN Search VARCHAR(255),
    IN Sort INT
);
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT *
    FROM Feeds
    WHERE Title LIKE '%' + Search + '%' OR Url LIKE '%' + Search + '%'
    ORDER BY 
        CASE WHEN Sort = 0 THEN Title END ASC,
        CASE WHEN Sort = 1 THEN Title END DESC,
        CASE WHEN Sort = 2 THEN Url END ASC,
        CASE WHEN Sort = 3 THEN Url END DESC,
        CASE WHEN Sort = 4 THEN CAST(CheckIntervals AS BIGINT) END ASC,
        CASE WHEN Sort = 5 THEN CAST(CheckIntervals AS BIGINT) END DESC
    OFFSET Start ROWS FETCH NEXT Length ROWS ONLY;
END

$$;
