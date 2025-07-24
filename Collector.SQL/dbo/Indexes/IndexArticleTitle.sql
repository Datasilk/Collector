CREATE INDEX [IndexArticleTitles]
	ON [dbo].Articles (title ASC)
GO
CREATE INDEX [IndexArticleTitlesDesc]
	ON [dbo].Articles (title DESC)