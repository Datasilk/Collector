CREATE OR REPLACE PROCEDURE  public."Domain_UpdateInfo"
(
    IN domainId INT DEFAULT 0,
    IN title VARCHAR(128),
    IN description VARCHAR(255),
    IN lang char(2) DEFAULT 'en'
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "title"=title, "description" = description, lang=lang, hastitle=1, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;
