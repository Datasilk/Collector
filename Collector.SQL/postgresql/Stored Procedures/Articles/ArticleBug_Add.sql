CREATE OR REPLACE FUNCTION public."ArticleBug_Add"
(
    p_articleId INT DEFAULT 0,
    p_title VARCHAR(100) DEFAULT '',
    p_description TEXT DEFAULT '',
    p_status SMALLINT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_bugId INT := nextval('public."SequenceArticleBugs"');
BEGIN
    INSERT INTO public."ArticleBugs" ("bugId", "articleId", "title", "description", "datecreated", "status")
    VALUES (v_bugId, p_articleId, p_title, p_description, CURRENT_TIMESTAMP, p_status);
END;
$$;