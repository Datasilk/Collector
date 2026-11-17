CREATE OR REPLACE FUNCTION public."Domain_DownloadRule_Add"
(
    p_domainId INT,
    p_rule BOOLEAN DEFAULT FALSE,
    p_url VARCHAR(64) DEFAULT '',
    p_title VARCHAR(64) DEFAULT '',
    p_summary VARCHAR(64) DEFAULT ''
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceDownloadRules"');
BEGIN
    INSERT INTO public."DownloadRules" ("ruleId", "domainId", "rule", "url", "title", "summary", "datecreated")
    VALUES (v_id, p_domainId, p_rule, p_url, p_title, p_summary, CURRENT_TIMESTAMP);

    RETURN v_id;
END;
$$;