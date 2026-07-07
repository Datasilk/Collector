CREATE OR REPLACE FUNCTION public."FeedCheckedLog_Add"
(
    p_feedId INT DEFAULT 0,
    p_links INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public."FeedsCheckedLog" ("feedId", "links", "datechecked")
    VALUES (p_feedId, p_links, CURRENT_TIMESTAMP);

    UPDATE public."Feeds" SET "lastChecked" = CURRENT_TIMESTAMP WHERE "feedId" = p_feedId;
END;
$$;