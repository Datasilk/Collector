CREATE OR REPLACE PROCEDURE  public."Article_Visited"
(
    IN articleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET visited += 1, cached = 1 WHERE articleId=articleId
END;

$$;
