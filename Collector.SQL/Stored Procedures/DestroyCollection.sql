CREATE OR REPLACE FUNCTION public."DestroyCollection"
(
    p_articles BOOLEAN DEFAULT TRUE,
    p_subjects BOOLEAN DEFAULT TRUE,
    p_topics BOOLEAN DEFAULT TRUE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_articles OR p_subjects THEN
        DELETE FROM public."ArticleBugs";
        DELETE FROM public."ArticleDates";
        DELETE FROM public."Articles";
        DELETE FROM public."ArticleSentences";
        DELETE FROM public."ArticleSubjects";
        DELETE FROM public."ArticleWords";
        DELETE FROM public."DownloadQueue";
        DELETE FROM public."Downloads";
        DELETE FROM public."FeedsCheckedLog";
    END IF;

    IF p_subjects THEN
        DELETE FROM public."Subjects";
        DELETE FROM public."Words";
    END IF;
END;
$$;