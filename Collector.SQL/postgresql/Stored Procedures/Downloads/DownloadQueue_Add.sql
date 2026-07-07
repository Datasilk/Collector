CREATE OR REPLACE FUNCTION public."DownloadQueue_Add"
(
    p_url TEXT DEFAULT '',
    p_domain VARCHAR(64) DEFAULT '',
    p_parentId INT,
    p_feedId INT DEFAULT 0
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
    v_qid BIGINT;
    v_title VARCHAR(128);
BEGIN
    IF EXISTS(SELECT 1 FROM public."Domains" d WHERE d."domain" = p_domain) THEN
        SELECT d."domainId", d."title" INTO v_domainId, v_title
        FROM public."Domains" d
        WHERE d."domain" = p_domain;

        IF v_title = '' THEN
            IF (SELECT COUNT(*) FROM public."Articles" a WHERE a."domainId" = v_domainId) >= 10 THEN
                v_title := public."Domain_FindTitle"(v_domainId);
            END IF;
        END IF;

        IF p_parentId > 0 AND p_parentId <> v_domainId THEN
            PERFORM public."DomainLink_Add"(p_parentId, v_domainId);
        END IF;
    ELSE
        SELECT public."Domain_Add"(p_domain, p_parentId) INTO v_domainId;
        SELECT d."title" INTO v_title FROM public."Domains" d WHERE d."domainId" = v_domainId;
    END IF;

    IF NOT EXISTS(SELECT 1 FROM public."DownloadQueue" q WHERE q."url" = p_url)
    AND NOT EXISTS(SELECT 1 FROM public."Downloads" d WHERE d."url" = p_url) THEN
        v_qid := nextval('public."SequenceDownloadQueue"');
        INSERT INTO public."DownloadQueue" ("qid", "url", "path", "feedId", "domainId", "status", "datecreated")
        VALUES (v_qid, p_url, public."GetPathFromUrl"(p_url, p_domain), p_feedId, v_domainId, 0, CURRENT_TIMESTAMP);
        UPDATE public."Domains" SET "inqueue" = "inqueue" + 1 WHERE "domainId" = v_domainId;
    ELSE
        SELECT q."qid" INTO v_qid FROM public."DownloadQueue" q WHERE q."url" = p_url;
        IF v_qid IS NULL THEN
            SELECT d."id" INTO v_qid FROM public."Downloads" d WHERE d."url" = p_url;
        END IF;
    END IF;

    RETURN v_qid;
END;
$$;