CREATE INDEX [IndexDomainServicesDomainId] ON [dbo].[DomainServices] ([domainId])
GO
CREATE INDEX [IndexDomainServicesServiceId] ON [dbo].[DomainServices] ([serviceId], [domainId])