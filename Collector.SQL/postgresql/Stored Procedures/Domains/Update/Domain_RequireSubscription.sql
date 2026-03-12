CREATE OR REPLACE PROCEDURE  public."Domain_RequireSubscription"
(
    IN domainId INT,
    IN required BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET paywall=required, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;
