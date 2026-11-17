CREATE TABLE IF NOT EXISTS public."StatisticsResearchers"
(
    "researcherId" INT NOT NULL PRIMARY KEY,
    "name" VARCHAR(100) NULL,
    "credentials" TEXT NULL,
    "bday" DATE NULL
);
