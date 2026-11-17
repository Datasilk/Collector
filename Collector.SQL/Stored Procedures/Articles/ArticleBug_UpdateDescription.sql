CREATE OR REPLACE FUNCTION public."ArticleBug_UpdateDescription"
(
    p_bugId INT DEFAULT 0,
    p_description TEXT DEFAULT ''
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."ArticleBugs" SET "description" = p_description WHERE "bugId" = p_bugId;
END;
$$;