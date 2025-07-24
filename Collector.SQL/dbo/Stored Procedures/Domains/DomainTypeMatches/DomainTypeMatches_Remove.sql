CREATE PROCEDURE [dbo].[DomainTypeMatches_Remove]
	@matchId int
AS
	DELETE FROM DomainTypeMatches WHERE matchId=@matchId