CREATE TABLE IF NOT EXISTS public."ConverseResources"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
ALTER TABLE public."ConverseResources"
    ADD CONSTRAINT "AK_ConverseResources_User" UNIQUE ("AppUserId", "ConverseId");
