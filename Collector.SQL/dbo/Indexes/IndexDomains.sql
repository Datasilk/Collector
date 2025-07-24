CREATE INDEX [IndexDomainNames] ON [dbo].[Domains] ([domain])
GO
CREATE INDEX [IndexDomainNamesDesc] ON [dbo].[Domains] ([domain] DESC)
GO
CREATE INDEX [IndexDomainsCreated] ON [dbo].[Domains] ([datecreated])
GO
CREATE INDEX [IndexDomainsLastChecked] ON [dbo].[Domains] ([lastchecked] DESC)
GO
CREATE INDEX [IndexDomainsCreatedDesc] ON [dbo].[Domains] ([datecreated] DESC)
GO
CREATE INDEX [IndexDomainArticles] ON [dbo].[Domains] ([articles] DESC)
GO
CREATE INDEX [IX_Domains_Title] ON [dbo].[Domains] ([title])
GO
CREATE INDEX [IX_Domains_HasTitle] ON [dbo].[Domains] ([hastitle] DESC)
GO
CREATE INDEX [IX_Domains_Language] ON [dbo].[Domains] ([lang])
INCLUDE ([domain], [paywall], [free])