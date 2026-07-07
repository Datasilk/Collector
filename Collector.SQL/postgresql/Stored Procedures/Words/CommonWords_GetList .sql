CREATE OR REPLACE FUNCTION public."CommonWords_GetList"()
RETURNS TABLE("word" VARCHAR(16))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."CommonWords";
END;
$$;