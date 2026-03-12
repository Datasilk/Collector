CREATE INDEX IF NOT EXISTS "IndexDomainServicesDomainId" ON public."DomainServices" ("domainId");

CREATE INDEX IF NOT EXISTS "IndexDomainServicesServiceId" ON public."DomainServices" ("serviceId", "domainId");
