CREATE PROCEDURE [dbo].[Download_UpdateType]
    @qId BIGINT,
    @type TINYINT
AS
BEGIN
    UPDATE [dbo].[DownloadQueue]
    SET [type] = @type
    WHERE [qid] = @qId
END
