CREATE OR REPLACE FUNCTION public."GetPathFromUrl"(
    url TEXT,
    domain VARCHAR(255)
)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    result TEXT;
    reverse_str TEXT;
    slash INT;
    domainPos INT;
BEGIN
    -- get path by removing all text to the left of the domain name (and removing the domain name too)
    domainPos := POSITION(domain IN url);
    IF domainPos = 0 THEN
        RETURN '';
    END IF;

    result := SUBSTRING(url FROM domainPos + LENGTH(domain));
    result := REPLACE(REPLACE(result, 'index.html', ''), 'index.php', '');

    -- remove query string
    IF POSITION('?' IN result) > 1 THEN
        result := SUBSTRING(result FROM 1 FOR POSITION('?' IN result) - 1);
    END IF;

    -- remove lingering slashes
    IF LENGTH(result) >= 1 AND LEFT(result, 1) = '/' THEN
        result := RIGHT(result, LENGTH(result) - 1);
    END IF;
    IF LENGTH(result) > 1 AND RIGHT(result, 1) = '/' THEN
        result := LEFT(result, LENGTH(result) - 1);
    END IF;

    -- check if file extension exists
    reverse_str := REVERSE(result);
    slash := POSITION('/' IN reverse_str);
    IF SUBSTRING(result FROM LENGTH(result) - 3 FOR 1) = '.' OR SUBSTRING(result FROM LENGTH(result) - 4 FOR 1) = '.' THEN
        -- reverse the result and find the last slash
        IF slash >= 1 THEN
            result := REVERSE(SUBSTRING(reverse_str FROM slash + 1 FOR LENGTH(reverse_str) - slash));
        ELSE
            result := '';
        END IF;
    END IF;

    -- finally, check if last item in path is too long to be part of path
    LOOP
        IF LENGTH(result) <= 1 THEN
            result := '';
            EXIT;
        END IF;
        reverse_str := REVERSE(result);
        slash := POSITION('/' IN reverse_str);
        IF slash >= 20 THEN
            result := REVERSE(SUBSTRING(reverse_str FROM slash + 1 FOR LENGTH(reverse_str) - slash));
        ELSE
            EXIT;
        END IF;
    END LOOP;

    RETURN result;
END;
$$;