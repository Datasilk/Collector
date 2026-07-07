CREATE OR REPLACE FUNCTION public."Feed_Checked"
(
    p_feedId INT DEFAULT 0
)
RETURNS INT
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."Feeds" SET "lastChecked" = CURRENT_TIMESTAMP WHERE "feedId" = p_feedId;
    RETURN 0;
END;
$$;