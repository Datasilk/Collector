CREATE OR REPLACE PROCEDURE  public."Blacklist_Wildcard_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO Blacklist_Wildcards (domain) VALUES (domain)
END;

$$;
