CREATE OR REPLACE FUNCTION public."Article_Add"
(
    p_feedId INT DEFAULT 0,
    p_subjects INT DEFAULT 0,
    p_subjectId INT DEFAULT 0,
    p_score SMALLINT DEFAULT 0,
    p_domain VARCHAR(50) DEFAULT '',
    p_url VARCHAR(250) DEFAULT '',
    p_title VARCHAR(250) DEFAULT '',
    p_summary VARCHAR(250) DEFAULT '',
    p_filesize DOUBLE PRECISION DEFAULT 0,
    p_linkcount INT DEFAULT 0,
    p_linkwordcount INT DEFAULT 0,
    p_wordcount INT DEFAULT 0,
    p_sentencecount SMALLINT DEFAULT 0,
    p_paragraphcount SMALLINT DEFAULT 0,
    p_importantcount SMALLINT DEFAULT 0,
    p_yearstart SMALLINT DEFAULT 0,
    p_yearend SMALLINT DEFAULT 0,
    p_years VARCHAR(50) DEFAULT '',
    p_images SMALLINT DEFAULT 0,
    p_datepublished TIMESTAMP DEFAULT NULL,
    p_relavance SMALLINT DEFAULT 1,
    p_importance SMALLINT DEFAULT 1,
    p_fiction SMALLINT DEFAULT 1,
    p_analyzed DOUBLE PRECISION DEFAULT 0.1,
    p_active BOOLEAN DEFAULT TRUE
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_articleId INT := NULL;
    v_domainId INT := NULL;
BEGIN
    SELECT d."domainId" INTO v_domainId
    FROM public."Domains" d
    WHERE d."domain" = p_domain;

    SELECT a."articleId" INTO v_articleId
    FROM public."Articles" a
    WHERE a."url" = p_url;

    IF v_domainId IS NULL THEN
        PERFORM public."Domain_Add"(p_domain, 0);

        SELECT d."domainId" INTO v_domainId
        FROM public."Domains" d
        WHERE d."domain" = p_domain;
    END IF;

    IF v_articleId IS NULL THEN
        v_articleId := nextval('public."SequenceArticles"');

        INSERT INTO public."Articles"
        ("articleId", "feedId", "subjects", "subjectId", "domainId", "score", "domain", "url", "title", "summary", "filesize", "linkcount", "linkwordcount", "wordcount", "sentencecount", "paragraphcount", "importantcount", "analyzecount",
        "yearstart", "yearend", "years", "images", "datecreated", "datepublished", "relavance", "importance", "fiction", "analyzed", "active")
        VALUES
        (v_articleId, p_feedId, p_subjects, p_subjectId, v_domainId, p_score, p_domain, p_url, p_title, p_summary, p_filesize, p_linkcount, p_linkwordcount, p_wordcount, p_sentencecount, p_paragraphcount, p_importantcount, 1,
        p_yearstart, p_yearend, p_years, p_images, CURRENT_TIMESTAMP, p_datepublished, p_relavance, p_importance, p_fiction, p_analyzed, p_active);

        UPDATE public."Domains" SET "articles" = "articles" + 1 WHERE "domainId" = v_domainId;
    END IF;

    UPDATE public."DownloadQueue" SET "status" = 0 WHERE "url" = p_url;

    RETURN v_articleId;
END;
$$;