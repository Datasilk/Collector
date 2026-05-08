-- Create function to find similar journal entries by title embedding
CREATE OR REPLACE FUNCTION public."JournalEntries_FindSimilar"(
    p_journalId INTEGER,
    p_embeddingJson TEXT,
    p_distance FLOAT DEFAULT 0.3
)
RETURNS TABLE (
    "Id" UUID,
    "Title" TEXT,
    "Distance" FLOAT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_queryVector VECTOR(768);
BEGIN
    -- Cast embedding JSON to vector once for reuse
    v_queryVector := CAST(p_embeddingJson AS VECTOR(768));
    
    RETURN QUERY
    SELECT 
        e."Id",
        e."Title",
        (e."Embedding" <=> v_queryVector)::FLOAT AS "Distance"
    FROM public."JournalEntries" e
    WHERE e."JournalId" = p_journalId
        AND e."Embedding" IS NOT NULL
        AND (e."Embedding" <=> v_queryVector) <= p_distance
    ORDER BY e."Embedding" <=> v_queryVector
    LIMIT 1;
END;
$$;
