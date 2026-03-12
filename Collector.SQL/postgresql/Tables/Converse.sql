CREATE TABLE IF NOT EXISTS public."Converse"
(
    "Id" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "TEXT" TEXT NOT NULL,
    "Who" BOOLEAN NOT NULL,
    "Created" TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."Converse"
    ADD CONSTRAINT "AK_Converse_User_Date" UNIQUE ("AppUserId", "Created");
