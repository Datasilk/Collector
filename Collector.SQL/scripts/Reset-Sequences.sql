-- =============================================
-- Reset All Sequences to MAX(Id) + 1
-- =============================================
-- This script calls the ResetAllSequences stored procedure
-- to reset all sequences in the database to the maximum ID value + 1
-- from their corresponding tables to prevent ID conflicts.
-- =============================================

EXEC [dbo].[ResetAllSequences]
