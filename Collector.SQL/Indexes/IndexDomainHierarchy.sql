CREATE INDEX IF NOT EXISTS "IX_DomainHierarchy_Domain" ON public."DomainHierarchy" ("domainId");

CREATE INDEX IF NOT EXISTS "IX_DomainHierarchy_Parent" ON public."DomainHierarchy" ("parentId", "level");
