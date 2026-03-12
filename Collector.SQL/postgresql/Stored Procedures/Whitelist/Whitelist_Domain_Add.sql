CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (domain)
	END TRY
	BEGIN CATCH
	END CATCH
END;

$$;
