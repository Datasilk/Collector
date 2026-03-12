CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Whitelist_Domains WHERE domain=domain
END;

$$;
