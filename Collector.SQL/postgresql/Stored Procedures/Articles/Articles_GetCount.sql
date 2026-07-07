CREATE OR REPLACE FUNCTION public."Articles_GetCount"
(
    p_subjectIds TEXT DEFAULT '',
    p_search TEXT DEFAULT '',
    p_feedId INT DEFAULT 0,
    p_domainId INT DEFAULT 0,
    p_score INT DEFAULT -9999,
    p_isActive INT DEFAULT 2,
    p_isDeleted BOOLEAN DEFAULT FALSE,
    p_minImages INT DEFAULT 0,
    p_dateStart TIMESTAMP(6) DEFAULT NULL,
    p_dateEnd TIMESTAMP(6) DEFAULT NULL,
    p_bugsOnly BOOLEAN DEFAULT FALSE
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_dateStart TIMESTAMP(6) := COALESCE(p_dateStart, CURRENT_TIMESTAMP - INTERVAL '100 years');
    v_dateEnd TIMESTAMP(6) := COALESCE(p_dateEnd, CURRENT_TIMESTAMP + INTERVAL '100 years');
    v_subjectIds INT[] := string_to_array(p_subjectIds, ',')::INT[];
    v_searchTerms TEXT[] := string_to_array(p_search, ',');
    v_subjectArticles INT[];
    v_searchArticles INT[];
BEGIN
    SELECT ARRAY(SELECT a."articleId") INTO v_subjectArticles
    FROM public."ArticleSubjects" a
    WHERE a."subjectId" = ANY(v_subjectIds)
    AND a."datecreated" >= v_dateStart AND a."datecreated" <= v_dateEnd;

    SELECT ARRAY(SELECT aw."articleId") INTO v_searchArticles
    FROM public."ArticleWords" aw
    WHERE aw."wordId" IN (
        SELECT w."wordId" FROM public."Words" w WHERE w."word" = ANY(v_searchTerms)
    );

    RETURN (SELECT COUNT(*)
    FROM public."Articles" a
    WHERE (
        a."articleId" = ANY(v_subjectArticles)
        OR a."articleId" = ANY(v_searchArticles)
        OR (p_search IS NOT NULL AND p_search <> '' AND (
            a."title" ILIKE '%' || p_search || '%'
            OR a."summary" ILIKE '%' || p_search || '%'
            OR a."url" ILIKE '%' || p_search || '%'
        ))
        OR (p_search IS NULL OR p_search = '')
    )
    AND (
        (p_feedId > 0 AND a."feedId" = p_feedId)
        OR p_feedId = 0
    )
    AND (
        (p_domainId > 0 AND a."domainId" = p_domainId)
        OR p_domainId = 0
    )
    AND a."active" = CASE WHEN p_isActive = 2 THEN a."active" ELSE p_isActive::BOOLEAN END
    AND a."deleted" = p_isDeleted
    AND a."score" >= p_score
    AND (
        (p_minImages > 0 AND a."images" >= p_minImages)
        OR p_minImages <= 0
    )
    AND a."datecreated" >= v_dateStart AND a."datecreated" <= v_dateEnd);
END;
$$;