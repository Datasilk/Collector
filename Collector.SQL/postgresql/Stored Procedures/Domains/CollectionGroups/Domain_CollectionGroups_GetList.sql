CREATE OR REPLACE FUNCTION public."Domain_CollectionGroups_GetList"()
RETURNS TABLE("colgroupId" INT, "name" VARCHAR(32))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."DomainCollectionGroups" ORDER BY "name" ASC;
END;
$$;