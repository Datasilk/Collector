CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRule_Add"
(
    IN domainId INT,
    IN selector varchar(64) DEFAULT '',
    IN rule BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceAnalyzerRules"');
BEGIN
INSERT INTO AnalyzerRules (ruleId, domainId, selector, "rule", datecreated)
	VALUES (id, domainId, selector, rule, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;
