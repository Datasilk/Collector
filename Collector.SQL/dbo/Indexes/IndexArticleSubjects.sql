CREATE INDEX [IndexArticleSubjects]
	ON [dbo].ArticleSubjects (subjectId ASC, datepublished DESC, datecreated DESC)