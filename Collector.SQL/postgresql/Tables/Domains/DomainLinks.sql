CREATE TABLE IF NOT EXISTS public."DomainLinks"
(
    "domainId" INT NOT NULL,
    "linkId" INT NOT NULL
);
ALTER TABLE public."DomainLinks"
    ADD CONSTRAINT PK_DomainLinks PRIMARY KEY ("domainId", "linkId");
