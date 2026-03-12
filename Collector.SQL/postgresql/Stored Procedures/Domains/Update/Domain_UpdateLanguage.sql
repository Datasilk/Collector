CREATE OR REPLACE PROCEDURE  public."Domain_UpdateLanguage"
(
    IN domainId INT DEFAULT 0,
    IN lang varchar(6)
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET lang = lang, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;
