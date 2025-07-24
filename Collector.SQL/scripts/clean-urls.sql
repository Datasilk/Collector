--removes back-slash / from end of url
UPDATE Downloads SET url = CASE WHEN SUBSTRING(url, LEN(url), 1) = '/' THEN SUBSTRING(url, 1, LEN(url) - 1)  ELSE url END