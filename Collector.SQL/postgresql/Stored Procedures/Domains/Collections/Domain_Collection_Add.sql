CREATE OR REPLACE FUNCTION public."Domain_Collection_Add"
(
    p_colgroupId INT DEFAULT 0,
    p_name VARCHAR(32),
    p_search VARCHAR(128),
    p_subjectId INT DEFAULT 0,
    p_filtertype INT DEFAULT 0,
    p_type INT DEFAULT 0,
    p_sort INT DEFAULT 0,
    p_lang VARCHAR(6) DEFAULT ''
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceDomainCollections"');
BEGIN
    INSERT INTO public."DomainCollections" ("colId", "colgroupId", "name", "search", "subjectId", "filtertype", "type", "sort", "lang", "datecreated")
    VALUES (v_id, p_colgroupId, p_name, p_search, p_subjectId, p_filtertype, p_type, p_sort, p_lang, CURRENT_TIMESTAMP);

    RETURN v_id;
END;
$$;