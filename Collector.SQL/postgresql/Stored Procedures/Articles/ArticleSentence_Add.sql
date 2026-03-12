CREATE OR REPLACE PROCEDURE  public."ArticleSentence_Add"
(
    IN articleId INT DEFAULT 0,
    IN index INT DEFAULT 0,
    IN sentence TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO ArticleSentences (articleId, "index", sentence)
	VALUES (articleId, index, sentence)
RETURN 0
END;

$$;
