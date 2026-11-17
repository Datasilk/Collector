CREATE OR REPLACE FUNCTION public."Blacklist_Wildcards_GetList"()
RETURNS TABLE("domain" VARCHAR(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT b."domain" FROM public."Blacklist_Wildcards" b ORDER BY b."domain" ASC;
END;
$$;