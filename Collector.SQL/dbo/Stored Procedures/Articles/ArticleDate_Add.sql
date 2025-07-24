CREATE PROCEDURE [dbo].[ArticleDate_Add]
	@articleId int = 0,
	@date date,
	@hasyear bit = 0,
	@hasmonth bit = 0,
	@hasday bit = 0
AS
	INSERT INTO ArticleDates (articleId, [date], hasyear, hasmonth, hasday)
	VALUES (@articleId, @date, @hasyear, @hasmonth, @hasday)
RETURN 0
