CREATE OR REPLACE PROCEDURE  public."Feeds_Category_Add"
(
    IN title VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceFeedCategories"');
BEGIN
INSERT INTO FeedCategories (categoryId, title) VALUES (id, title)
END;

$$;
