CREATE OR REPLACE PROCEDURE  public."Words_GetList"
(
    IN words TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #words FROM public.SplitArray(words, ',')
SELECT w.* FROM Words w
WHERE word IN (SELECT value FROM #words)
END;

$$;
