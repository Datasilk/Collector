CREATE OR REPLACE PROCEDURE  public."Domain_UpdateHttpsWww"
(
    IN domainId INT DEFAULT 0,
    IN https BOOLEAN DEFAULT FALSE,
    IN www BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "https" = https, "www" = www, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;
