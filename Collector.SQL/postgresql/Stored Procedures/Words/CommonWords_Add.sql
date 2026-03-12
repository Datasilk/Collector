CREATE OR REPLACE PROCEDURE  public."CommonWords_Add"
(
    IN words TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM CommonWords WHERE word IN (SELECT "value" FROM public.SplitArray(words, ','))
INSERT INTO CommonWords SELECT "value" AS word FROM public.SplitArray(words, ',')
END;

$$;
