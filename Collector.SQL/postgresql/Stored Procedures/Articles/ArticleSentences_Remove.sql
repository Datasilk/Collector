CREATE OR REPLACE PROCEDURE  public."ArticleSentences_Remove"
(
    IN articleId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM ArticleSentences WHERE articleId=articleId
RETURN 0
END;

$$;
