CREATE TABLE IF NOT EXISTS public."StatisticsResults"
(
    "statId" INT NOT NULL PRIMARY KEY,
    "projectId" INT NULL,
    "year" INT NULL,
    "month" INT NULL,
    "day" INT NULL,
    "test" DOUBLE PRECISION NULL,
    "result" DOUBLE PRECISION NULL,
    "country" VARCHAR(3) NULL,
    "city" VARCHAR(45) NULL,
    "state" VARCHAR(5) NULL,
    "topic" VARCHAR(50) NULL,
    "target" VARCHAR(50) NULL,
    "sentence" VARCHAR(250) NULL
);
