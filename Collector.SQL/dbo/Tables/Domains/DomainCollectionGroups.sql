
CREATE TABLE [dbo].[DomainCollectionGroups]
(
	[colgroupId] INT NOT NULL PRIMARY KEY, 
    [name] NVARCHAR(32) NOT NULL
)
GO

CREATE INDEX [IX_Domains_DomainCollectionGroupNames] ON [dbo].[DomainCollectionGroups] ([name] DESC)