DROP FUNCTION IF EXISTS public."Domains_GetList";

CREATE FUNCTION public."Domains_GetList"
(
    p_subjectIds TEXT DEFAULT '',
    p_lang VARCHAR(6) DEFAULT '',
    p_search TEXT DEFAULT '',
    p_type INT DEFAULT 0,
    p_domainType INT DEFAULT -1,
    p_domainType2 INT DEFAULT -1,
    p_sort INT DEFAULT 0,
    p_start INT DEFAULT 1,
    p_length INT DEFAULT 50,
    p_parentId INT DEFAULT -1,
    p_serviceIds TEXT DEFAULT NULL
)
RETURNS TABLE(
    "rownum" BIGINT, "domainId" INT, "domain" VARCHAR(64), "lang" VARCHAR(6), "parentId" INT,
    "hastitle" BOOLEAN, "paywall" BOOLEAN, "free" BOOLEAN, "https" BOOLEAN, "www" BOOLEAN,
    "empty" BOOLEAN, "deleted" BOOLEAN, "type" INT, "type2" INT, "articles" INT, "inqueue" INT,
    "title" VARCHAR(128), "company" VARCHAR(64), "description" VARCHAR(255), "datecreated" TIMESTAMPTZ,
    "dateupdated" TIMESTAMPTZ, "lastchecked" TIMESTAMPTZ, "whitelisted" INT, "blacklisted" INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_haswildcard BOOLEAN := FALSE;
    v_serviceArr INT[];
BEGIN
    IF POSITION('%' IN p_search) > 0 THEN
        v_haswildcard := TRUE;
    END IF;
    v_serviceArr := string_to_array(p_serviceIds, ',')::INT[];

    IF p_type = 2 THEN
        RETURN QUERY
        SELECT * FROM (
            SELECT ROW_NUMBER() OVER(ORDER BY
                CASE WHEN p_sort = 0 OR p_sort = 6 THEN d."domain" END,
                CASE WHEN p_sort = 1 OR p_sort = 7 THEN d."domain" END DESC
            ) AS rn,
            NULL::INT, d."domain", NULL::VARCHAR(6), NULL::INT,
            NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN,
            NULL::BOOLEAN, NULL::BOOLEAN, -1, NULL::INT, NULL::INT, NULL::INT,
            NULL::VARCHAR(128), NULL::VARCHAR(64), NULL::VARCHAR(255), NULL::TIMESTAMPTZ,
            NULL::TIMESTAMPTZ, NULL::TIMESTAMPTZ, NULL::INT, NULL::INT
            FROM public."Blacklist_Domains" d
            WHERE (
                (p_search IS NOT NULL AND p_search <> '' AND d."domain" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END)
                OR (p_search IS NULL OR p_search = '')
            )
        ) AS tbl WHERE rn >= p_start AND rn < p_start + p_length;
    ELSIF p_type = 8 THEN
        RETURN QUERY
        SELECT * FROM (
            SELECT ROW_NUMBER() OVER(ORDER BY
                CASE WHEN p_sort = 0 OR p_sort = 6 THEN d."domain" END,
                CASE WHEN p_sort = 1 OR p_sort = 7 THEN d."domain" END DESC
            ) AS rn,
            NULL::INT, d."domain", NULL::VARCHAR(6), NULL::INT,
            NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN, NULL::BOOLEAN,
            NULL::BOOLEAN, NULL::BOOLEAN, -2, NULL::INT, NULL::INT, NULL::INT,
            NULL::VARCHAR(128), NULL::VARCHAR(64), NULL::VARCHAR(255), NULL::TIMESTAMPTZ,
            NULL::TIMESTAMPTZ, NULL::TIMESTAMPTZ, NULL::INT, NULL::INT
            FROM public."Blacklist_Wildcards" d
            WHERE (
                (p_search IS NOT NULL AND p_search <> '' AND d."domain" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END)
                OR (p_search IS NULL OR p_search = '')
            )
        ) AS tbl WHERE rn >= p_start AND rn < p_start + p_length;
    ELSE
        RETURN QUERY
        SELECT * FROM (
            SELECT ROW_NUMBER() OVER(ORDER BY
                CASE WHEN p_sort = 0 OR p_sort = 1 OR p_sort = 6 OR p_sort = 7 THEN d."hastitle" END DESC,
                CASE WHEN p_sort = 6 THEN d."title" END,
                CASE WHEN p_sort = 7 THEN d."title" END DESC,
                CASE WHEN p_sort = 0 THEN d."domain" END,
                CASE WHEN p_sort = 1 THEN d."domain" END DESC,
                CASE WHEN p_sort = 2 THEN d."articles" END DESC,
                CASE WHEN p_sort = 8 THEN d."articles" END ASC,
                CASE WHEN p_sort = 3 THEN d."datecreated" END DESC,
                CASE WHEN p_sort = 4 THEN d."datecreated" END ASC,
                CASE WHEN p_sort = 5 THEN d."dateupdated" END DESC,
                CASE WHEN p_sort = 9 THEN d."dateupdated" END ASC,
                CASE WHEN p_sort = 10 THEN
                    CASE WHEN wl."domain" IS NOT NULL THEN 1 WHEN bl."domain" IS NOT NULL THEN 2 ELSE 3 END
                END ASC,
                CASE WHEN p_sort = 11 THEN
                    CASE WHEN wl."domain" IS NOT NULL THEN 1 WHEN bl."domain" IS NOT NULL THEN 2 ELSE 3 END
                END DESC
            ) AS rn,
            d."domainId", d."domain", d."lang", d."parentId", d."hastitle", d."paywall", d."free", d."https", d."www",
            d."empty", d."deleted", d."type", d."type2", d."articles", d."inqueue", d."title", d."company",
            d."description", d."datecreated", d."dateupdated", d."lastchecked",
            (CASE WHEN wl."domain" IS NOT NULL THEN 1 ELSE 0 END) AS wl,
            (CASE WHEN bl."domain" IS NOT NULL THEN 1 ELSE 0 END) AS bl
            FROM public."Domains" d
            LEFT JOIN public."Whitelist_Domains" wl ON wl."domain" = d."domain"
            LEFT JOIN public."Blacklist_Domains" bl ON bl."domain" = d."domain"
            LEFT JOIN public."DomainServices" ds ON ds."domainId" = d."domainId" AND ds."serviceId" = ANY(v_serviceArr)
            WHERE
            (
                (p_search IS NOT NULL AND p_search <> '' AND (
                    d."title" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END
                    OR d."domain" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END
                ))
                OR (p_search IS NULL OR p_search = '')
            )
            AND (
                (p_type = 0)
                OR (p_type = 1 AND wl."domain" IS NOT NULL)
                OR (p_type = 2 AND bl."domain" IS NOT NULL)
                OR (p_type = 3 AND wl."domain" IS NULL AND bl."domain" IS NULL)
                OR (p_type = 4 AND d."paywall" = TRUE)
                OR (p_type = 5 AND d."free" = TRUE)
                OR (p_type = 6 AND d."free" = FALSE AND d."paywall" = FALSE AND d."type" = -1 AND bl."domain" IS NULL AND wl."domain" IS NULL)
                OR (p_type = 7 AND d."empty" = TRUE)
                OR (p_type = 9 AND d."empty" = FALSE)
            )
            AND (
                (p_sort = 2 AND d."articles" > 0)
                OR (p_sort <> 2)
            )
            AND (
                (p_domainType >= 0 AND p_domainType2 < 0 AND (d."type" = p_domainType OR d."type2" = p_domainType))
                OR
                (p_domainType < 0 AND p_domainType2 >= 0 AND (d."type" = p_domainType2 OR d."type2" = p_domainType2))
                OR
                (p_domainType >= 0 AND p_domainType2 >= 0 AND (d."type" = p_domainType OR d."type2" = p_domainType
                                                              OR d."type" = p_domainType2 OR d."type2" = p_domainType2))
                OR
                (p_domainType < 0)
            )
            AND (
                (p_parentId >= 0 AND d."parentId" = p_parentId)
                OR (p_parentId < 0)
            )
            AND (
                (p_lang <> '' AND d."lang" = p_lang)
                OR p_lang IS NULL OR p_lang = ''
            )
            AND d."deleted" = FALSE
            AND (
                p_serviceIds IS NULL
                OR ds."serviceId" IS NOT NULL
            )
        ) AS tbl WHERE rn >= p_start AND rn < p_start + p_length;
    END IF;
END;
$$;