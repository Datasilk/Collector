CREATE OR REPLACE PROCEDURE  public."Subjects_GetList"
(
    IN subjectIds TEXT,
    IN parentId INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
IF subjectIds <> '' BEGIN
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	SELECT * FROM Subjects 
	WHERE subjectId IN (SELECT CONVERT(INT, value) FROM #subjects)
	AND parentId = CASE WHEN parentId >= 0 THEN parentId ELSE parentId END
	ORDER BY title ASC
END ELSE BEGIN
/* parentId only */
	SELECT * FROM Subjects 
	WHERE parentId = CASE WHEN parentId >= 0 THEN parentId ELSE parentId END
	ORDER BY title ASC
END
END;

$$;
