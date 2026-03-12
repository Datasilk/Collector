CREATE OR REPLACE PROCEDURE  public."Article_UpdateCache"
(
    IN articleId INT DEFAULT 0,
    IN cached BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET cached=cached WHERE articleId=articleId
END;

$$;
