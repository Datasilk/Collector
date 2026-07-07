CREATE OR REPLACE FUNCTION public."Feeds_Category_Add"
(
    p_title VARCHAR(64)
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceFeedCategories"');
BEGIN
    INSERT INTO public."FeedCategories" ("categoryId", "title") VALUES (v_id, p_title);
END;
$$;