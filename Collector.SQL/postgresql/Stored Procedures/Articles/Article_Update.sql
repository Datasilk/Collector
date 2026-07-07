CREATE OR REPLACE FUNCTION public."Article_Update"
(
    p_articleId INT DEFAULT 0,
    p_subjects INT DEFAULT 0,
    p_subjectId INT DEFAULT 0,
    p_score SMALLINT DEFAULT 0,
    p_title VARCHAR(250),
    p_summary VARCHAR(250),
    p_filesize DOUBLE PRECISION DEFAULT 0,
    p_wordcount INT DEFAULT 0,
    p_sentencecount INT DEFAULT 0,
    p_paragraphcount INT DEFAULT 0,
    p_importantcount INT DEFAULT 0,
    p_yearstart INT DEFAULT 0,
    p_yearend INT DEFAULT 0,
    p_years VARCHAR(50),
    p_images SMALLINT DEFAULT 0,
    p_datepublished TIMESTAMP,
    p_relavance SMALLINT DEFAULT 1,
    p_importance SMALLINT DEFAULT 1,
    p_fiction SMALLINT DEFAULT 1,
    p_analyzed DOUBLE PRECISION DEFAULT 0.1
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Articles"
    SET "subjects" = p_subjects,
        "subjectId" = p_subjectId,
        "score" = p_score,
        "title" = p_title,
        "summary" = p_summary,
        "filesize" = p_filesize,
        "wordcount" = p_wordcount,
        "sentencecount" = p_sentencecount,
        "paragraphcount" = p_paragraphcount,
        "importantcount" = p_importantcount,
        "analyzecount" = "analyzecount" + 1,
        "yearstart" = p_yearstart,
        "yearend" = p_yearend,
        "years" = p_years,
        "images" = p_images,
        "datepublished" = p_datepublished,
        "relavance" = p_relavance,
        "importance" = p_importance,
        "fiction" = p_fiction,
        "analyzed" = p_analyzed
    WHERE "articleId" = p_articleId;
END;
$$;