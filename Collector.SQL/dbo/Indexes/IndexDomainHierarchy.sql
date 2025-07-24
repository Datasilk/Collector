CREATE INDEX [IX_DomainHierarchy_Domain] ON [dbo].[DomainHierarchy] ([domainId])
GO
CREATE INDEX [IX_DomainHierarchy_Parent] ON [dbo].[DomainHierarchy] ([parentId], [level])