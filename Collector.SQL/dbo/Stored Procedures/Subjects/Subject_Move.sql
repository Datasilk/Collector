CREATE PROCEDURE [dbo].[Subject_Move]
	@subjectId int = 1,
	@newParent int = 127
AS
	DECLARE 
	@title NVARCHAR(50) = '',
	@bread NVARCHAR(500) = '', 
	@hier NVARCHAR(50), 
	@newBread NVARCHAR(500) = '',
	@newHier NVARCHAR(50),
	@newTitle NVARCHAR(50),
	@cursor1 CURSOR,
	@childId INT, @parentId INT,
	@parentTitle NVARCHAR(50),
	@parentHier NVARCHAR(50),
	@parentBread NVARCHAR(500)

	/* get breadcrumb info */
	SELECT @bread = breadcrumb, @hier = hierarchy FROM Subjects WHERE subjectId=@subjectId
	IF @bread <> '' BEGIN
		SET @bread = @bread + '>' + @title
		SET @hier = @hier + '>' + CONVERT(NVARCHAR(25),@subjectId)
	END ELSE BEGIN
		SET @bread = @title
		SET @hier = CONVERT(NVARCHAR(25),@subjectId)
	END
	SELECT @newBread = breadcrumb, @newHier = hierarchy, @newTitle=title FROM Subjects WHERE subjectId=@newParent
	IF @newBread <> '' BEGIN
		SET @newBread = @newBread + '>' + @newTitle
		SET @newHier = @newHier + '>' + CONVERT(NVARCHAR(25),@newParent)
	END ELSE BEGIN
		SET @newBread = @newTitle
		SET @newHier = CONVERT(NVARCHAR(25),@newParent)
	END

	/* update subject */
	UPDATE Subjects 
	SET parentId=@newParent, hierarchy=@newHier, breadcrumb=@newBread 
	WHERE subjectId=@subjectId

	/* update each child subject */
	SET @cursor1 = CURSOR FOR
	SELECT subjectId, parentId FROM Subjects WHERE hierarchy LIKE @hier + '>%' OR hierarchy = @hier ORDER BY hierarchy ASC
	OPEN @cursor1
	FETCH FROM @cursor1 INTO
	@childId, @parentId
	WHILE @@FETCH_STATUS = 0
    BEGIN
		SELECT @parentTitle = title, @parentHier=hierarchy, @parentBread=breadcrumb FROM Subjects WHERE subjectId=@parentId
		IF @parentBread <> '' BEGIN
			SET @parentBread = @parentBread + '>' + @parentTitle
			SET @parentHier = @parentHier + '>' + CONVERT(NVARCHAR(25),@parentId)
		END ELSE BEGIN
			SET @parentBread = @parentTitle
			SET @parentHier = CONVERT(NVARCHAR(25),@parentId)
		END
		UPDATE Subjects SET hierarchy=@parentHier, breadcrumb=@parentBread WHERE subjectId=@childId

		FETCH FROM @cursor1 INTO
		@childId, @parentId
	END

	CLOSE @cursor1
	DEALLOCATE @cursor1

	
