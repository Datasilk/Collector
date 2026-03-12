CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_Add"
(
    IN domainId INT,
    IN rule BOOLEAN DEFAULT FALSE,
    IN url varchar(64) DEFAULT '',
    IN title varchar(64) DEFAULT '',
    IN summary varchar(64) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDownloadRules"');
BEGIN
INSERT INTO DownloadRules (ruleId, domainId, "rule", "url", title, summary, datecreated)
	VALUES (id, domainId, rule, url, title, summary, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;
