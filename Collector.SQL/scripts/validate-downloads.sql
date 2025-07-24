-- delete all downloads that do not have a valid URL
DELETE FROM DownloadQueue
WHERE qid IN
(
	SELECT q.qid FROM DownloadQueue q
	JOIN Domains d ON d.domainId = q.domainId
	WHERE (
		LEN(q.url) <= LEN(d.domain) + 9
		OR q.url NOT LIKE 'http%'
		OR q.url LIKE '%.jpg'
		OR q.url LIKE '%.jpeg'
		OR q.url LIKE '%.svg'
		--OR q.url LIKE '%.pdf'
		OR q.url LIKE '%.zip'
	)
)

UPDATE DownloadQueue SET status=0
WHERE qid IN (
SELECT qid FROM DownloadQueue q
JOIN Domains d ON d.domainId = q.domainId
WHERE LEN(q.url) <= LEN(d.domain) + 9)

SELECT TOP 100 * FROM DownloadQueue q
JOIN Domains d ON d.domainId = q.domainId
WHERE LEN(q.url) <= LEN(d.domain) + 10

/*
INSERT INTO DownloadQueue
SELECT q.id AS qid, q.feedId, q.domainId, q.status, q.tries, q.url, q.path, q.datecreated 
FROM Downloads q

DELETE FROM Downloads
*/