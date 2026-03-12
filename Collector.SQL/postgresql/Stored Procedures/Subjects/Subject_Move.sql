CREATE OR REPLACE PROCEDURE  public."Subject_Move"
(
    IN subjectId INT DEFAULT 1,
    IN newParent INT DEFAULT 127
);
LANGUAGE plpgsql
AS $$
BEGIN
DECLARE 
	title VARCHAR(50) = '',
	bread VARCHAR(500) = '', 
	hier VARCHAR(50), 
	newBread VARCHAR(500) = '',
	newHier VARCHAR(50),
	newTitle VARCHAR(50),
	cursor1 CURSOR,
	childId INT, parentId INT,
	parentTitle VARCHAR(50),
	parentHier VARCHAR(50),
	parentBread VARCHAR(500)
	/* get breadcrumb info */
	SELECT bread = breadcrumb, hier = hierarchy FROM Subjects WHERE subjectId=subjectId
	IF bread <> '' BEGIN
		SET bread = bread + '>' + title
		SET hier = hier + '>' + CONVERT(VARCHAR(25),subjectId)
	END ELSE BEGIN
		SET bread = title
		SET hier = CONVERT(VARCHAR(25),subjectId)
	END
	SELECT newBread = breadcrumb, newHier = hierarchy, newTitle=title FROM Subjects WHERE subjectId=newParent
	IF newBread <> '' BEGIN
		SET newBread = newBread + '>' + newTitle
		SET newHier = newHier + '>' + CONVERT(VARCHAR(25),newParent)
	END ELSE BEGIN
		SET newBread = newTitle
		SET newHier = CONVERT(VARCHAR(25),newParent)
	END
	/* update subject */
	UPDATE Subjects 
	SET parentId=newParent, hierarchy=newHier, breadcrumb=newBread 
	WHERE subjectId=subjectId
	/* update each child subject */
	SET cursor1 = CURSOR FOR
	SELECT subjectId, parentId FROM Subjects WHERE hierarchy LIKE hier + '>%' OR hierarchy = hier ORDER BY hierarchy ASC
	OPEN cursor1
	FETCH FROM cursor1 INTO
	childId, parentId
	WHILE @@FETCH_STATUS = 0
    BEGIN
		SELECT parentTitle = title, parentHier=hierarchy, parentBread=breadcrumb FROM Subjects WHERE subjectId=parentId
		IF parentBread <> '' BEGIN
			SET parentBread = parentBread + '>' + parentTitle
			SET parentHier = parentHier + '>' + CONVERT(VARCHAR(25),parentId)
		END ELSE BEGIN
			SET parentBread = parentTitle
			SET parentHier = CONVERT(VARCHAR(25),parentId)
		END
		UPDATE Subjects SET hierarchy=parentHier, breadcrumb=parentBread WHERE subjectId=childId
		FETCH FROM cursor1 INTO
		childId, parentId
	END
	CLOSE cursor1
	DEALLOCATE cursor1
END;

$$;
