CREATE OR REPLACE FUNCTION public."Article_Exists"
(
    p_url VARCHAR(250)
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM public."Articles" WHERE "url" = p_url);
END;
$$;