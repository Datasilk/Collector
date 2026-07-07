CREATE OR REPLACE FUNCTION public."Article_UpdateCache"
(
    p_articleId INT DEFAULT 0,
    p_cached BOOLEAN DEFAULT TRUE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Articles" SET "cached" = p_cached WHERE "articleId" = p_articleId;
END;
$$;