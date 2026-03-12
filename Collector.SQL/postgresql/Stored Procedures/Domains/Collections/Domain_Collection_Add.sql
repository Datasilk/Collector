CREATE OR REPLACE PROCEDURE  public."Domain_Collection_Add"
(
    IN colgroupId INT DEFAULT 0,
    IN name VARCHAR(32),
    IN search VARCHAR(128),
    IN subjectId INT DEFAULT 0,
    IN filtertype INT DEFAULT 0,
    IN type INT DEFAULT 0,
    IN sort INT DEFAULT 0,
    IN lang varchar(6) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomainCollections"');
BEGIN
INSERT INTO DomainCollections (colId, colgroupId, "name", "search", subjectId, filtertype, "type", "sort", lang, datecreated)
	VALUES (id, colgroupId, name, search, subjectId, filtertype, type, sort, lang, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;
