CREATE TABLE IF NOT EXISTS public."ConverseSubjects"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'AK_ConverseSubjects_User_Subject'
    ) THEN
        ALTER TABLE public."ConverseSubjects"
            ADD CONSTRAINT "AK_ConverseSubjects_User_Subject" UNIQUE ("AppUserId", "SubjectId", "ConverseId");
    END IF;
END $$;
