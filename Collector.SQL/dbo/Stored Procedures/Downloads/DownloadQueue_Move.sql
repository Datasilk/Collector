CREATE PROCEDURE [dbo].[DownloadQueue_Move]
	@qid bigint = 0
AS
	--move related Download Queue record into Downloads table
	INSERT INTO Downloads ([id], [feedId], [domainId], [status], [tries], [url], [path], [datecreated]) 
	SELECT * FROM DownloadQueue WHERE qid=@qid
	DELETE FROM DownloadQueue WHERE qid=@qid

	