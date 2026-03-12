CREATE OR REPLACE PROCEDURE  public."Blacklist_Wildcard_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Blacklist_Wildcards WHERE domain=domain
END;

$$;
