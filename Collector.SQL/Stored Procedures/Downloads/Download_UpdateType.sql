CREATE OR REPLACE FUNCTION public."Download_UpdateType"
(
    p_qId BIGINT,
    p_type SMALLINT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."DownloadQueue" SET "type" = p_type WHERE "qid" = p_qId;
END;
$$;