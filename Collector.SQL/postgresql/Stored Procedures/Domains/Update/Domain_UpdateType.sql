CREATE OR REPLACE PROCEDURE  public."Domain_UpdateType"
(
    IN domainId INT DEFAULT 0,
    IN type INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "type" = type, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;
