CREATE OR REPLACE FUNCTION public."Domains_GetCount"
(
    p_subjectIds TEXT DEFAULT '',
    p_lang VARCHAR(6) DEFAULT '',
    p_search TEXT DEFAULT '',
    p_type INT DEFAULT 0,
    p_domainType INT DEFAULT -1,
    p_domainType2 INT DEFAULT -1,
    p_sort INT DEFAULT 0,
    p_parentId INT DEFAULT -1,
    p_serviceIds TEXT DEFAULT NULL
)
RETURNS INT
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
        RETURN (
            SELECT COUNT(*)
            FROM public."Blacklist_Domains" d
            WHERE (
                (p_search IS NOT NULL AND p_search <> '' AND d."domain" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END)
                OR (p_search IS NULL OR p_search = '')
            )
        );
    ELSIF p_type = 8 THEN
        RETURN (
            SELECT COUNT(*)
            FROM public."Blacklist_Wildcards" d
            WHERE (
                (p_search IS NOT NULL AND p_search <> '' AND d."domain" ILIKE CASE WHEN v_haswildcard THEN p_search ELSE '%' || p_search || '%' END)
                OR (p_search IS NULL OR p_search = '')
            )
        );
    ELSE
        RETURN (
            SELECT COUNT(*)
            FROM public."Domains" d
            LEFT JOIN public."Whitelist_Domains" wl ON wl."domain" = d."domain"
            LEFT JOIN public."Blacklist_Domains" bl ON bl."domain" = d."domain"
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
                OR EXISTS (
                    SELECT 1 FROM public."DomainServices" ds
                    WHERE ds."domainId" = d."domainId"
                    AND ds."serviceId" = ANY(v_serviceArr)
                )
            )
        );
    END IF;
END;
$$;