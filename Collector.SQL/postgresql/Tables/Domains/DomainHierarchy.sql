CREATE TABLE IF NOT EXISTS public."DomainHierarchy"
(
    "domainId" INT NOT NULL,
    "parentId" INT NOT NULL,
    "level" INT NOT NULL
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'public."DomainHierarchy"'::regclass AND contype = 'p'
    ) THEN
        ALTER TABLE public."DomainHierarchy"
            ADD CONSTRAINT PK_DomainHierarchy PRIMARY KEY ("domainId", "parentId");
    END IF;
END $$;
