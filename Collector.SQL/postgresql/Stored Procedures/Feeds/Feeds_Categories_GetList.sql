CREATE OR REPLACE PROCEDURE public."Feeds_Categories_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM FeedCategories ORDER BY title ASC
END;

$$;
