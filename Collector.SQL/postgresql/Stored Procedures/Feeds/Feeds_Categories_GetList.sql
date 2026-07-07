CREATE OR REPLACE FUNCTION public."Feeds_Categories_GetList"()
RETURNS TABLE("categoryId" INT, "title" VARCHAR(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public."FeedCategories" ORDER BY "title" ASC;
END;
$$;