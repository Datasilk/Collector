CREATE INDEX [IndexArticleUrl]
	ON [dbo].Articles (url ASC)
GO
CREATE INDEX [IndexArticleUrlDesc]
	ON [dbo].Articles (url DESC)