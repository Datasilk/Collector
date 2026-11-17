CREATE OR REPLACE FUNCTION public."Domain_Collection_Remove"
(
    p_colId INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public."DomainCollections" WHERE "colId" = p_colId;
END;
$$;