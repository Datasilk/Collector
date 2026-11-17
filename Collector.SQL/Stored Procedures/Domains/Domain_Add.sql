CREATE OR REPLACE FUNCTION public."Domain_Add"
(
    p_domain VARCHAR(64),
    p_title VARCHAR(128) DEFAULT '',
    p_parentId INT DEFAULT 0,
    p_type INT DEFAULT 0
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceDomains"');
    v_level INT;
    v_url TEXT := 'http://' || p_domain;
BEGIN
    INSERT INTO public."Domains" ("domainId", "parentId", "domain", "title", "lastchecked")
    VALUES (v_id, p_parentId, p_domain, p_title, CURRENT_TIMESTAMP - INTERVAL '1 hour');

    IF p_parentId > 0 THEN
        BEGIN
            INSERT INTO public."DomainHierarchy" ("domainId", "parentId", "level")
            SELECT v_id, p_parentId, dh."level"
            FROM public."DomainHierarchy" dh WHERE dh."domainId" = p_parentId;
        EXCEPTION
            WHEN unique_violation THEN
                NULL;
        END;

        SELECT COALESCE(MAX(dh."level"), 0) + 1 INTO v_level
        FROM public."DomainHierarchy" dh
        WHERE dh."domainId" = p_parentId;

        BEGIN
            INSERT INTO public."DomainHierarchy" ("domainId", "parentId", "level")
            VALUES (v_id, p_parentId, v_level);
        EXCEPTION
            WHEN unique_violation THEN
                NULL;
        END;

        PERFORM public."DomainLink_Add"(p_parentId, v_id);
    END IF;

    IF p_type = 1 THEN
        PERFORM public."Whitelist_Domain_Add"(p_domain);
    ELSIF p_type = 2 THEN
        PERFORM public."Blacklist_Domain_Add"(p_domain);
    END IF;

    PERFORM public."DownloadQueue_Add"(v_url, p_domain, p_parentId, 0);

    RETURN v_id;
END;
$$;