CREATE PROCEDURE [dbo].[DownloadQueue_MoveArchived]
AS
	INSERT INTO Downloads ([id], [feedId], [domainId], [type], [status], [tries], [url], [path], [datecreated]) 
	SELECT * FROM DownloadQueue WHERE [status]=2 AND NOT EXISTS(SELECT * FROM Downloads WHERE id=qid)
	DELETE FROM DownloadQueue WHERE [status]=2
	