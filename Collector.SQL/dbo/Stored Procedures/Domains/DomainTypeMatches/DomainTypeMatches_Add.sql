CREATE PROCEDURE [dbo].[DomainTypeMatches_Add]
	@type int,
	@type2 int = -1,
	@words nvarchar(MAX),
	@threshold int,
	@rank int
AS
	DECLARE @id int
	SET @id = NEXT VALUE FOR SequenceDomainTypeMatches
	INSERT INTO DomainTypeMatches (matchId, [type], [type2], words, threshold, [rank])
	VALUES (@id, @type, @type2, @words, @threshold, @rank)