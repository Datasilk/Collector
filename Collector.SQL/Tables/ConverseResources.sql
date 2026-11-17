CREATE TABLE IF NOT EXISTS public."ConverseResources"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'AK_ConverseResources_User'
    ) THEN
        ALTER TABLE public."ConverseResources"
            ADD CONSTRAINT "AK_ConverseResources_User" UNIQUE ("AppUserId", "ConverseId");
    END IF;
END $$;
