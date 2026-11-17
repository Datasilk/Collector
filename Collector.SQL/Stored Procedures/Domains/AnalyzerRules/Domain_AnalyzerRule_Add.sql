CREATE OR REPLACE FUNCTION public."Domain_AnalyzerRule_Add"
(
    p_domainId INT,
    p_selector VARCHAR(64) DEFAULT '',
    p_rule BOOLEAN DEFAULT FALSE
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT := nextval('public."SequenceAnalyzerRules"');
BEGIN
    INSERT INTO public."AnalyzerRules" ("ruleId", "domainId", "selector", "rule", "datecreated")
    VALUES (v_id, p_domainId, p_selector, p_rule, CURRENT_TIMESTAMP);

    RETURN v_id;
END;
$$;