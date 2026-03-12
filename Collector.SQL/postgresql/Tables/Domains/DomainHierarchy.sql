CREATE TABLE IF NOT EXISTS public."DomainHierarchy"
(
    "domainId" INT NOT NULL,
    "parentId" INT NOT NULL,
    "level" INT NOT NULL
);
ALTER TABLE public."DomainHierarchy"
    ADD CONSTRAINT PK_DomainHierarchy PRIMARY KEY ("domainId", "parentId");
