CREATE OR REPLACE FUNCTION public."Chat_GetContext"(
    p_appUserId UUID,
    p_embeddingJson TEXT,
    p_length INTEGER DEFAULT 5,
    p_distance DOUBLE PRECISION DEFAULT NULL
);
RETURNS TABLE (
    "Content" TEXT,
    "Metadata" TEXT,
    "Distance" DOUBLE PRECISION
);
LANGUAGE plpgsql
AS $$
DECLARE
    v_queryVector VECTOR(768);
BEGIN
    -- Cast embedding JSON to vector once for reuse
    v_queryVector := CAST(p_embeddingJson AS VECTOR(768));
    
    -- If distance filter is provided, use it to narrow results
    IF p_distance IS NOT NULL THEN
        RETURN QUERY
        SELECT 
            c."Content",
            c."Metadata",
            (c."Embedding" <=> v_queryVector)::DOUBLE PRECISION AS "Distance"
        FROM public."ChatContextChunks" c
        WHERE c."AppUserId" = p_appUserId
            AND (c."Embedding" <=> v_queryVector) <= p_distance
        ORDER BY c."Embedding" <=> v_queryVector
        LIMIT p_length;
    ELSE
        -- No distance filter, return top K results
        RETURN QUERY
        SELECT 
            c."Content",
            c."Metadata",
            (c."Embedding" <=> v_queryVector)::DOUBLE PRECISION AS "Distance"
        FROM public."ChatContextChunks" c
        WHERE c."AppUserId" = p_appUserId
        ORDER BY c."Embedding" <=> v_queryVector
        LIMIT p_length;
    END IF;
END;
$$;
