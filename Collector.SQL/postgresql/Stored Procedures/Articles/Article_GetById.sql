CREATE OR REPLACE PROCEDURE  public."Article_GetById"
(
    IN articleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT a.*, d.domain FROM Articles a
	LEFT JOIN Domains d ON d.domainId = a.domainId
	WHERE a.articleId=articleId
RETURN 0
END;

$$;
