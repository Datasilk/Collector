CREATE TABLE IF NOT EXISTS public."Dictionary"
(
    "word" VARCHAR(25) NOT NULL PRIMARY KEY,
    "vocabtype" SMALLINT NULL,
    "grammertype" SMALLINT NULL,
    "socialtype" SMALLINT NULL,
    "objecttype" SMALLINT NULL,
    "score" SMALLINT NULL
);
