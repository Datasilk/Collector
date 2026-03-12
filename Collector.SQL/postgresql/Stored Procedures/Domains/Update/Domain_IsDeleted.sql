CREATE OR REPLACE PROCEDURE  public."Domain_IsDeleted"
(
    IN domainId INT,
    IN delete BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET "deleted"=delete, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;
