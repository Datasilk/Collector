CREATE TABLE IF NOT EXISTS public."DomainLinks"
(
    "domainId" INT NOT NULL,
    "linkId" INT NOT NULL
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'public."DomainLinks"'::regclass AND contype = 'p'
    ) THEN
        ALTER TABLE public."DomainLinks"
            ADD CONSTRAINT PK_DomainLinks PRIMARY KEY ("domainId", "linkId");
    END IF;
END $$;
