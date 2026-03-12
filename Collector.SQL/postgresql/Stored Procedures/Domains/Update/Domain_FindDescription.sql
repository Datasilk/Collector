CREATE OR REPLACE PROCEDURE  public."Domain_FindDescription"
(
    IN domainId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    description VARCHAR(255);
BEGIN
SELECT TOP 1 description = summary
	FROM Articles 
	WHERE domainId=domainId
	AND summary != ''
	ORDER BY LEN(url) ASC
	UPDATE Domains SET "description" = description, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
	SELECT description
END;

$$;
