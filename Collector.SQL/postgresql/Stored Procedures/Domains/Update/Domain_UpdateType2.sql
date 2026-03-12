CREATE OR REPLACE PROCEDURE  public."Domain_UpdateType2"
(
    IN domainId INT DEFAULT 0,
    IN type INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "type2" = type, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;
