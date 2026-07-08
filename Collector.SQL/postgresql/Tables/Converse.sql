CREATE TABLE IF NOT EXISTS public."Converse"
(
    "Id" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "TEXT" TEXT NOT NULL,
    "Who" BOOLEAN NOT NULL,
    "Created" TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP
);
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'AK_Converse_User_Date'
    ) THEN
        ALTER TABLE public."Converse"
            ADD CONSTRAINT "AK_Converse_User_Date" UNIQUE ("AppUserId", "Created");
    END IF;
END $$;
