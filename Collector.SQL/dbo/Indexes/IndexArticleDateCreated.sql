CREATE INDEX [IndexArticleDateCreated]
	ON [dbo].Articles (datecreated ASC)
GO
CREATE INDEX [IndexArticleDateCreatedDesc]
	ON [dbo].Articles (datecreated DESC)