CREATE OR REPLACE FUNCTION public."ArticleDate_Add"
(
    p_articleId INT DEFAULT 0,
    p_date DATE,
    p_hasYear BOOLEAN DEFAULT FALSE,
    p_hasMonth BOOLEAN DEFAULT FALSE,
    p_hasDay BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."ArticleDates" ("articleId", "date", "hasyear", "hasmonth", "hasday")
    VALUES (p_articleId, p_date, p_hasYear, p_hasMonth, p_hasDay);
END;
$$;