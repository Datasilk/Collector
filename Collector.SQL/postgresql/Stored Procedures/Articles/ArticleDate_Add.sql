CREATE OR REPLACE PROCEDURE  public."ArticleDate_Add"
(
    IN articleId INT DEFAULT 0,
    IN date date,
    IN hasyear BOOLEAN DEFAULT FALSE,
    IN hasmonth BOOLEAN DEFAULT FALSE,
    IN hasday BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO ArticleDates (articleId, "date", hasyear, hasmonth, hasday)
	VALUES (articleId, date, hasyear, hasmonth, hasday)
RETURN 0
END;

$$;
