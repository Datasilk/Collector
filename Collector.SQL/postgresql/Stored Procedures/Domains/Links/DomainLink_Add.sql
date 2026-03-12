CREATE OR REPLACE PROCEDURE  public."DomainLink_Add"
(
    IN domainId INT,
    IN linkId INT
);
LANGUAGE plpgsql
AS $$
BEGIN TRY
		INSERT INTO DomainLinks (domainId, linkId) VALUES (domainId, linkId)
	END TRY BEGIN CATCH END CATCH
END;

$$;
