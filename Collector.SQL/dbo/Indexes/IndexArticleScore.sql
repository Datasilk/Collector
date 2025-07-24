CREATE INDEX [IndexArticleScore]
	ON [dbo].Articles (score ASC)

GO

CREATE INDEX [IndexArticleScoreDesc]
	ON [dbo].Articles (score DESC)