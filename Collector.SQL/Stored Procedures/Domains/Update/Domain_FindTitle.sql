CREATE OR REPLACE FUNCTION public."Domain_FindTitle"
(
    p_domainId INT DEFAULT 0
)
RETURNS VARCHAR(128)
LANGUAGE plpgsql
AS $$
DECLARE
    v_domainTitle VARCHAR(128);
    v_domain VARCHAR(64);
    v_domainpart VARCHAR(64);
    v_domainpart2 VARCHAR(64);
    v_word TEXT;
    v_title VARCHAR(250);
    v_count INT;
    v_exclude TEXT[] := ARRAY['and', 'or', '&', 'the', 'for', 'with'];
    v_domainparts TEXT[];
    cur CURSOR FOR SELECT a."title" FROM public."Articles" a WHERE a."domainId" = p_domainId LIMIT 100;
BEGIN
    SELECT d."domain" INTO v_domain FROM public."Domains" d WHERE d."domainId" = p_domainId;

    DROP TABLE IF EXISTS tmp_words;
    CREATE TEMP TABLE tmp_words (word TEXT) ON COMMIT DROP;

    OPEN cur;
    LOOP
        FETCH cur INTO v_title;
        EXIT WHEN NOT FOUND;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, ' '))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, '-'))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, '|'))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, ':'))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, ';'))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;

        FOR v_word IN SELECT TRIM(unnest(string_to_array(v_title, '/'))) LOOP
            IF LENGTH(v_word) > 2 AND NOT (v_word = ANY(v_exclude)) THEN
                INSERT INTO tmp_words VALUES (v_word);
            END IF;
        END LOOP;
    END LOOP;
    CLOSE cur;

    SELECT COUNT(*) INTO v_count FROM tmp_words;

    v_domainparts := string_to_array(v_domain, '.');
    SELECT REPLACE("value", '-', '%') INTO v_domainpart
    FROM unnest(v_domainparts) AS "value"
    LIMIT 1;

    SELECT STRING_AGG("value", '') INTO v_domainpart2
    FROM unnest(v_domainparts) AS "value";

    SELECT TRIM(w.word) INTO v_domainTitle
    FROM tmp_words w
    GROUP BY w.word
    HAVING COUNT(w.word) > 1
    ORDER BY
        CASE
            WHEN REPLACE(w.word, ' ', '') ILIKE v_domainpart || '%' THEN 50
            WHEN REPLACE(w.word, ' ', '') ILIKE v_domainpart2 || '%' THEN 100
            ELSE 0
        END DESC,
        COUNT(w.word) DESC,
        LENGTH(w.word) DESC
    LIMIT 1;

    UPDATE public."Domains"
    SET "title" = v_domainTitle, "hastitle" = TRUE, "dateupdated" = CURRENT_TIMESTAMP
    WHERE "domainId" = p_domainId;

    DROP TABLE IF EXISTS tmp_words;

    RETURN v_domainTitle;
END;
$$;