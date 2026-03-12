CREATE OR REPLACE PROCEDURE  public."Feeds_GetListWithLogs"
(
    IN days INT DEFAULT 7,
    IN dateStart date
);
LANGUAGE plpgsql
AS $$
DECLARE
    tblresults TABLE (;
BEGIN
DECLARE 
	cursor1 CURSOR,
	cursor2 CURSOR,
	feedId INT,
	title VARCHAR(100),
	url VARCHAR(100),
	checkIntervals INT = 720,
	lastChecked TIMESTAMP,
	filter TEXT,
	logfeedId INT,
	loglinks smallint,
	logdatechecked TIMESTAMP
		feedId INT NOT NULL,
		title VARCHAR(100) NULL,
		url VARCHAR(100) NULL,
		checkIntervals INT,
		lastChecked TIMESTAMP NULL,
		filter TEXT NULL,
		loglinks smallint NULL,
		logdatechecked TIMESTAMP NULL
	);
	SET cursor1 = CURSOR FOR 
	SELECT * FROM feeds WHERE feedId > 0 ORDER BY checkIntervals ASC, title ASC
	OPEN cursor1
	FETCH FROM cursor1 INTO
	feedId, title, url, checkIntervals, lastChecked, filter
	WHILE @@FETCH_STATUS = 0 BEGIN
		/*add feed to results table */
		INSERT INTO tblresults (feedId, title, url, checkIntervals, lastChecked, filter)
		VALUES (feedId, title, url, checkIntervals, lastChecked, filter)
		/* get log data for each feed */
		SET cursor2 = CURSOR FOR 
		SELECT * FROM FeedsCheckedLog 
		WHERE feedId=feedId 
		AND datechecked >= dateStart
		AND datechecked <= DATEADD(DAY, days, dateStart)
		ORDER BY datechecked ASC
		OPEN cursor2
		FETCH FROM cursor2 INTO
		logfeedId, loglinks, logdatechecked
		WHILE @@FETCH_STATUS = 0 BEGIN
			/* add feed log record to results table */
			INSERT INTO tblresults (feedId, loglinks, logdatechecked)
			VALUES(feedId, loglinks, logdatechecked)
			FETCH FROM cursor2 INTO
			logfeedId, loglinks, logdatechecked
		END
		CLOSE cursor2
		DEALLOCATE cursor2
		FETCH FROM cursor1 INTO
		feedId, title, url, checkIntervals, lastChecked, filter
	END
	CLOSE cursor1
	DEALLOCATE cursor1
	/* finally, return results */
	SELECT * FROM tblresults
END;

$$;
