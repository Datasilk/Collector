CREATE OR REPLACE PROCEDURE  public."Domain_Collection_Remove"
(
    IN colId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainCollections WHERE colId=colId
END;

$$;
