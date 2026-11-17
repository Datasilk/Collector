CREATE TABLE IF NOT EXISTS public."DomainCollectionGroups"
(
    "colgroupId" INT NOT NULL PRIMARY KEY,
    "name" VARCHAR(32) NOT NULL
);CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionGroupNames" ON public."DomainCollectionGroups" ("name" DESC);
