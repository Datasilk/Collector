CREATE OR REPLACE FUNCTION public."Feed_Add"
(
    p_doctype INT DEFAULT 1,
    p_categoryId INT,
    p_title VARCHAR(100) DEFAULT '',
    p_url VARCHAR(100) DEFAULT '',
    p_domain VARCHAR(64) DEFAULT '',
    p_filter TEXT DEFAULT '',
    p_checkIntervals INT DEFAULT 720
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainId INT;
    v_feedId INT := nextval('public."SequenceFeeds"');
BEGIN
    IF EXISTS(SELECT 1 FROM public."Domains" d WHERE d."domain" = p_domain) THEN
        SELECT d."domainId" INTO v_domainId FROM public."Domains" d WHERE d."domain" = p_domain;
    ELSE
        v_domainId := nextval('public."SequenceDomains"');
        INSERT INTO public."Domains" ("domainId", "parentId", "domain", "lastchecked")
        VALUES (v_domainId, 0, p_domain, CURRENT_TIMESTAMP - INTERVAL '1 hour');
    END IF;

    INSERT INTO public."Feeds" ("feedId", "domainId", "doctype", "categoryId", "title", "url", "checkIntervals", "filter", "lastChecked")
    VALUES (v_feedId, v_domainId, p_doctype, p_categoryId, p_title, p_url, p_checkIntervals, p_filter, CURRENT_TIMESTAMP - INTERVAL '24 hours');

    BEGIN
        INSERT INTO public."Whitelist_Domains" ("domain") VALUES (p_domain);
    EXCEPTION
        WHEN unique_violation THEN
            NULL;
    END;

    RETURN v_feedId;
END;
$$;