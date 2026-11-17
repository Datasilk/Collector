CREATE OR REPLACE FUNCTION public."Download_Update"
(
    p_qid BIGINT DEFAULT 0,
    p_status INT DEFAULT 0
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."DownloadQueue" SET "status" = p_status WHERE "qid" = p_qid;
END;
$$;