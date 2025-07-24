CREATE PROCEDURE [dbo].[Feed_Checked]
	@feedId int = 0
AS
	UPDATE Feeds SET lastChecked=GETUTCDATE() WHERE feedId=@feedId
RETURN 0
