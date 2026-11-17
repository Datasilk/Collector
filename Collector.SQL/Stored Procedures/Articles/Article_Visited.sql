CREATE OR REPLACE FUNCTION public."Article_Visited"
(
    p_articleId INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Articles" SET "visited" = "visited" + 1, "cached" = TRUE WHERE "articleId" = p_articleId;
END;
$$;