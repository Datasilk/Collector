CREATE FUNCTION [dbo].[GetPathFromUrl]
(
	@url nvarchar(MAX),
	@domain nvarchar(255)
)
RETURNS nvarchar(MAX)
AS
BEGIN
	-- get path by removing all text to the left of the domain name (and removing the domain name too)
	DECLARE @result nvarchar(MAX) = REPLACE(REPLACE(SUBSTRING(@url, CHARINDEX(@domain, @url) + LEN(@domain) + 1, LEN(@url) - CHARINDEX(@domain, @url) + LEN(@domain)), 'index.html', ''), 'index.php', '')
	
	--remove query string
	IF CHARINDEX('?', @result, 1) > 1 BEGIN
		SET @result = SUBSTRING(@result, 1, CHARINDEX('?', @result, 1) - 1)
	END
	
	-- next, remove lingering slashes
	IF LEN(@result) > =1 AND LEFT(@result, 1) = '/' SET @result = RIGHT(@result, LEN(@result) - 1)--first slash
	IF LEN(@result) > 1 AND RIGHT(@result, 1) = '/' SET @result = LEFT(@result, LEN(@result) - 1) --last slash

	-- next, check if file extension exists
	DECLARE @reverse nvarchar(MAX) = REVERSE(@result)
	DECLARE @slash int = CHARINDEX('/', @reverse)
	IF(SUBSTRING(@result, LEN(@result) - 3, 1) = '.' OR SUBSTRING(@result, LEN(@result) - 4, 1) = '.') BEGIN
		-- next, reverse the result and find the last slash
		IF @slash >= 1 BEGIN
			SET @result = REVERSE(SUBSTRING(@reverse, @slash + 1, LEN(@reverse) - @slash))
		END ELSE BEGIN
			SET @result = ''
		END
	END
	
	-- next, remove the last item in the path
	--SET @reverse = REVERSE(@result)
	--SET @slash = CHARINDEX('/', @reverse)
	--IF @slash >= 4 BEGIN
	--	SET @result = REVERSE(SUBSTRING(@reverse, @slash + 1, LEN(@reverse) - @slash))
	--END ELSE BEGIN
	--	SET @result = ''
	--END

	-- finally, check if last item in path is too long to be part of path
	WHILE 1 = 1 BEGIN
		IF LEN(@result) <= 1 BEGIN
			SET @result = ''
			BREAK
		END
		SET @reverse = REVERSE(@result)
		SET @slash = CHARINDEX('/', @reverse)
		IF @slash >= 20 BEGIN
			SET @result = REVERSE(SUBSTRING(@reverse, @slash + 1, LEN(@reverse) - @slash))
		END ELSE BEGIN
			BREAK
		END
	END
	RETURN @result
END
GO