DECLARE @result nvarchar(MAX)
SELECT @result = 'INSERT INTO Blacklist_Domains VALUES ' + CHAR(10) + (
	SELECT '''' + domain + ''',' + CHAR(10)  AS [text()] 
	FROM Blacklist_Domains
	WHERE domain <> ''
	FOR XML PATH (''), TYPE
).value('text()[1]','nvarchar(max)')
PRINT @result