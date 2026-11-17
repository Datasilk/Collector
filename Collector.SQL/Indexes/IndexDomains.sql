CREATE INDEX IF NOT EXISTS "IndexDomainNames" ON public."Domains" ("domain");

CREATE INDEX IF NOT EXISTS "IndexDomainNamesDesc" ON public."Domains" ("domain" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainsCreated" ON public."Domains" ("datecreated");

CREATE INDEX IF NOT EXISTS "IndexDomainsLastChecked" ON public."Domains" ("lastchecked" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainsCreatedDesc" ON public."Domains" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainArticles" ON public."Domains" ("articles" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_Title" ON public."Domains" ("title");

CREATE INDEX IF NOT EXISTS "IX_Domains_HasTitle" ON public."Domains" ("hastitle" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_Language" ON public."Domains" ("lang")
    INCLUDE ("domain", "paywall", "free");
