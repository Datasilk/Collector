CREATE OR REPLACE PROCEDURE  public."Article_Update"
(
    IN articleId INT DEFAULT 0,
    IN subjects INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN score smallint DEFAULT 0,
    IN title VARCHAR(250),
    IN summary VARCHAR(250),
    filesize DOUBLE PRECISION = 0,
    IN wordcount INT DEFAULT 0,
    IN sentencecount INT DEFAULT 0,
    IN paragraphcount INT DEFAULT 0,
    IN importantcount INT DEFAULT 0,
    IN yearstart INT DEFAULT 0,
    IN yearend INT DEFAULT 0,
    IN years VARCHAR(50),
    IN images smallint DEFAULT 0,
    IN datepublished TIMESTAMP,
    IN relavance smallint DEFAULT 1,
    IN importance smallint DEFAULT 1,
    IN fiction smallint DEFAULT 1,
    analyzed DOUBLE PRECISION = 0.1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET 
subjects=subjects, subjectId=subjectId, score=score, title=title, summary=summary, filesize=filesize, wordcount=wordcount, sentencecount=sentencecount,
paragraphcount=paragraphcount, importantcount=importantcount, analyzecount=analyzecount+1, 
yearstart=yearstart, yearend=yearend, years=years, images=images, datepublished=datepublished, 
relavance=relavance, importance=importance, fiction=fiction, analyzed=analyzed
WHERE articleId=articleId
END;

$$;
