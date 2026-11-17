CREATE OR REPLACE FUNCTION public."DownloadQueue_Check"
(
    p_domaindelay INT DEFAULT 60,
    p_domain VARCHAR(64) DEFAULT '',
    p_feedId INT DEFAULT 0,
    p_sort INT DEFAULT 0,
    p_qid BIGINT DEFAULT 0
)
RETURNS TABLE(
    "qid" BIGINT, "feedId" INT, "domainId" INT, "type" SMALLINT, "status" INT,
    "tries" INT, "url" VARCHAR(255), "path" VARCHAR(255), "datecreated" TIMESTAMP(6),
    "domain" VARCHAR(64), "articles" INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_qid BIGINT := 0;
    v_domainId INT;
    v_maxQid BIGINT := 0;
    v_randQid BIGINT;
BEGIN
    IF p_qid = 0 THEN
        IF p_sort IN (2, 3) THEN
            SELECT COALESCE(MAX(q."qid"), 0) INTO v_maxQid FROM public."DownloadQueue" q;
            v_randQid := floor(random() * v_maxQid)::BIGINT;
        END IF;

        IF p_domain IS NOT NULL AND p_domain <> '' THEN
            SELECT d."domainId" INTO v_domainId FROM public."Domains" d WHERE d."domain" = p_domain;
        END IF;

        SELECT q."qid", q."domainId"
        INTO v_qid, v_domainId
        FROM public."DownloadQueue" q
        JOIN public."Domains" d ON d."domainId" = q."domainId"
        LEFT JOIN public."Whitelist_Domains" w ON w."domain" = d."domain"
        LEFT JOIN public."Blacklist_Domains" b ON b."domain" = d."domain"
        WHERE
            (p_domain IS NULL OR p_domain = '' OR q."domainId" = v_domainId)
            AND q."domainId" NOT IN (
                SELECT d2."domainId" FROM public."Domains" d2
                WHERE d2."lastchecked" >= CURRENT_TIMESTAMP - (p_domaindelay || ' seconds')::INTERVAL
            )
            AND (
                (p_feedId > 0 AND q."feedId" = p_feedId)
                OR p_feedId <= 0
            )
            AND (d."paywall" = FALSE OR (d."paywall" = TRUE AND d."free" = TRUE))
            AND (
                ((p_sort IN (2, 3)) AND v_maxQid > 0 AND q."qid" >= v_randQid)
                OR v_maxQid = 0
            )
            AND (
                (p_sort = 2 AND LENGTH(q."url") <= LENGTH(d."domain") + 11)
                OR p_sort != 2
            )
            AND (
                (p_sort != 2 AND w."domain" IS NOT NULL)
                OR p_sort = 2
            )
            AND b."domain" IS NULL
            AND q."status" = 0
            AND (d."lang" = '' OR d."lang" = 'en')
        ORDER BY
            CASE WHEN p_sort = 0 THEN q."datecreated" END DESC
        LIMIT 1;
    ELSE
        v_qid := p_qid;
        SELECT q."domainId" INTO v_domainId FROM public."DownloadQueue" q WHERE q."qid" = p_qid;
    END IF;

    IF v_qid IS NULL OR v_qid = 0 OR v_domainId IS NULL THEN
        RETURN;
    END IF;

    UPDATE public."DownloadQueue" dq SET "status" = 1 WHERE dq."qid" = v_qid;
    UPDATE public."Domains" d SET "lastchecked" = CURRENT_TIMESTAMP WHERE d."domainId" = v_domainId;

    RETURN QUERY
    SELECT q.*, d."domain", d."articles"
    FROM public."DownloadQueue" q
    JOIN public."Domains" d ON d."domainId" = q."domainId"
    WHERE q."qid" = v_qid;
END;
$$;