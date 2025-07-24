CREATE PROCEDURE [dbo].[DomainLink_Add]
	@domainId int,
	@linkId int
AS
	BEGIN TRY
		INSERT INTO DomainLinks (domainId, linkId) VALUES (@domainId, @linkId)
	END TRY BEGIN CATCH END CATCH