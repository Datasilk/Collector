CREATE OR REPLACE FUNCTION public."ArticleBug_UpdateStatus"
(
    p_bugId INT DEFAULT 0,
    p_status INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."ArticleBugs" SET "status" = p_status WHERE "bugId" = p_bugId;
END;
$$;