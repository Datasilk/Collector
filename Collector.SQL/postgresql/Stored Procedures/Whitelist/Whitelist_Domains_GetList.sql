CREATE OR REPLACE FUNCTION public."Whitelist_Domains_GetList"()
RETURNS TABLE("domain" VARCHAR(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT w."domain" FROM public."Whitelist_Domains" w ORDER BY w."domain" ASC;
END;
$$;