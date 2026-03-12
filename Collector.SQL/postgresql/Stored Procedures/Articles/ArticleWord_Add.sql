CREATE OR REPLACE PROCEDURE  public."ArticleWord_Add"
(
    IN articleId INT DEFAULT 0,
    IN wordId INT DEFAULT 0,
    IN count INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF (SELECT COUNT(*) FROM ArticleWords WHERE articleId=articleId AND wordId=wordId) = 0 BEGIN
		INSERT INTO ArticleWords (articleId, wordId, "count") 
		VALUES (articleId, wordId, count)
	END
END;

$$;
