CREATE TABLE [dbo].[DomainHierarchy]
(
	[domainId] INT NOT NULL, 
    [parentId] INT NOT NULL,
    [level] INT NOT NULL,
    CONSTRAINT PK_DomainHierarchy PRIMARY KEY (domainId, parentId)
)
