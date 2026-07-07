CREATE OR REPLACE FUNCTION public."Downloads_GetCount"()
RETURNS INT
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM public."Downloads");
END;
$$;