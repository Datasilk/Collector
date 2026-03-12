CREATE OR REPLACE PROCEDURE  public."Domain_IsEmpty"
(
    IN domainId INT,
    IN empty BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET "empty"=empty, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;
