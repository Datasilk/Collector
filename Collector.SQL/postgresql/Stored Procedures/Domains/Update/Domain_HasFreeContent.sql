CREATE OR REPLACE PROCEDURE  public."Domain_HasFreeContent"
(
    IN domainId INT,
    IN free BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET free=free, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;
