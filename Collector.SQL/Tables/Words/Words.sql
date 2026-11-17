CREATE TABLE IF NOT EXISTS public."Words"
(
    "wordId" INT NOT NULL PRIMARY KEY,
    "word" VARCHAR(64) NOT NULL,
    "grammartype" INT NULL,
    "score" INT NULL
);
