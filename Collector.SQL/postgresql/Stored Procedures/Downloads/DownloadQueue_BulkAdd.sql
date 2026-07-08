CREATE OR REPLACE FUNCTION public."DownloadQueue_BulkAdd"
(
    p_urls TEXT DEFAULT '',
    p_domain VARCHAR(64) DEFAULT '',
    p_parentId INT DEFAULT 0,
    p_feedId INT DEFAULT 0
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_url TEXT;
    v_domainId INT;
    v_qid BIGINT;
    v_count INT := 0;
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

    FOR v_url IN SELECT DISTINCT TRIM(unnest(string_to_array(p_urls, ','))) LOOP
        IF v_url = '' THEN CONTINUE; END IF;

        IF NOT EXISTS(SELECT 1 FROM public."DownloadQueue" q WHERE q."url" = v_url)
        AND NOT EXISTS(SELECT 1 FROM public."Downloads" d WHERE d."url" = v_url)
        AND NOT EXISTS(SELECT 1 FROM public."Articles" a WHERE a."url" = v_url) THEN
            v_qid := nextval('public."SequenceDownloadQueue"');
            INSERT INTO public."DownloadQueue" ("qid", "url", "path", "feedId", "domainId", "status", "datecreated")
            VALUES (v_qid, v_url, public."GetPathFromUrl"(v_url, p_domain), p_feedId, v_domainId, 0, CURRENT_TIMESTAMP);
            v_count := v_count + 1;
        END IF;
    END LOOP;

    UPDATE public."Domains" SET "inqueue" = "inqueue" + v_count WHERE "domainId" = v_domainId;
    RETURN v_count;
END;
$$;