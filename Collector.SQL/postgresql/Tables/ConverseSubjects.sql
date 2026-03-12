CREATE TABLE IF NOT EXISTS public."ConverseSubjects"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
ALTER TABLE public."ConverseSubjects"
    ADD CONSTRAINT "AK_ConverseSubjects_User_Subject" UNIQUE ("AppUserId", "SubjectId", "ConverseId");
