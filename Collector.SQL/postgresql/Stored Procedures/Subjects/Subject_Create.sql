CREATE OR REPLACE PROCEDURE  public."Subject_Create"
(
    IN parentId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 0,
    IN title VARCHAR(50),
    IN breadcrumb TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    create BOOLEAN := 1, hierarchy VARCHAR(50) = '';
    id INT := nextval('public."SequenceSubjects"');
BEGIN
IF parentId > 0 BEGIN
		IF (SELECT COUNT(*) FROM Subjects WHERE breadcrumb = breadcrumb AND title=title) > 0 BEGIN
			/* subject already exists */
			SET create = 0
		END ELSE BEGIN
			/* get hierarchy indexes */
			SELECT hierarchy = hierarchy FROM Subjects WHERE subjectId=parentId
			if hierarchy <> '' BEGIN
			 SET hierarchy = hierarchy  + '>' + CONVERT(VARCHAR(10),parentId)
			END ELSE BEGIN
			 SET hierarchy =  CONVERT(VARCHAR(10),parentId)
			END
		END
	END ELSE BEGIN
		IF (SELECT COUNT(*) FROM Subjects WHERE parentId=0 AND title=title) > 0 BEGIN
			/* root subject already exists */
			SET create = 0
		END
	END
	IF create = 1 BEGIN
		/* finally, create subject */
		INSERT INTO Subjects (subjectId, parentId, grammartype, score, title, breadcrumb, hierarchy)
		VALUES (id, parentId, grammartype, score, title, breadcrumb, hierarchy)
		SELECT id
	END ELSE BEGIN
		SELECT 0
	END
END;

$$;
