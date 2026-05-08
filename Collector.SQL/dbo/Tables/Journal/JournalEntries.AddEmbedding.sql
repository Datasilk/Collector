-- Add embedding column to JournalEntries table for vector similarity search
ALTER TABLE public."JournalEntries"
ADD COLUMN "Embedding" VECTOR(768);

-- Create index for vector similarity queries
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_Embedding" 
ON public."JournalEntries" 
USING ivfflat ("Embedding" vector_cosine_ops);