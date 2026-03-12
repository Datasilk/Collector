CREATE OR REPLACE PROCEDURE public."Domain_CollectionGroups_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DomainCollectionGroups ORDER BY "name" ASC
END;

$$;
