using System;
using System.IO;

namespace Collector.Common
{
    /// <summary>
    /// Relative-path builders for the per-entry layout under the journal storage root
    /// (Files.Paths.Journal):
    ///   journals/{journalId}/{entryId}/entry.json
    ///   journals/{journalId}/{entryId}/research.json
    ///   journals/{journalId}/{entryId}/snapshots/{id}.json
    ///   journals/{journalId}/{entryId}/research/{####}.json
    ///   journals/{journalId}/{entryId}/images/{file}
    ///   journals/{journalId}/{entryId}/files/{file}
    ///   journals/{journalId}/{entryId}/videos/{videoId}/raw.mp4|thumb.jpg|hls/|previews/
    /// All methods return forward-slash relative paths (safe to store in JSON/DB).
    /// </summary>
    public static class JournalPaths
    {
        // Entry dirs on disk are N-format GUIDs (no hyphens) — match migrate-storage.
        private static string E(Guid entryId) => entryId.ToString("N");
        private static string E(string entryId) =>
            Guid.TryParse(entryId, out var g) ? g.ToString("N") : entryId;

        public static string EntryDir(int journalId, Guid entryId) => $"{journalId}/{E(entryId)}";
        public static string EntryDir(int journalId, string entryId) => $"{journalId}/{E(entryId)}";

        public static string ImagePath(int journalId, Guid entryId, string fileName) =>
            $"{journalId}/{E(entryId)}/images/{fileName}";
        public static string ImagePath(int journalId, string entryId, string fileName) =>
            $"{journalId}/{E(entryId)}/images/{fileName}";

        public static string FilePath(int journalId, Guid entryId, string fileName) =>
            $"{journalId}/{E(entryId)}/files/{fileName}";

        public static string VideoDir(int journalId, Guid entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}";
        public static string VideoDir(int journalId, string entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}";

        public static string VideoPath(int journalId, Guid entryId, string videoId, string extension = ".mp4") =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/raw{extension}";
        public static string VideoPath(int journalId, string entryId, string videoId, string extension = ".mp4") =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/raw{extension}";

        public static string VideoThumbPath(int journalId, Guid entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/thumb.jpg";
        public static string VideoThumbPath(int journalId, string entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/thumb.jpg";

        public static string VideoHlsDir(int journalId, Guid entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/hls";
        public static string VideoHlsDir(int journalId, string entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/hls";

        public static string VideoPreviewsDir(int journalId, Guid entryId, string videoId) =>
            $"{journalId}/{E(entryId)}/videos/{videoId}/previews";

        /// <summary>The videoId for a stored video — the filename stem (GUID).</summary>
        public static string VideoIdFromFileName(string fileName) =>
            Path.GetFileNameWithoutExtension(fileName);

        /// <summary>Rewrites a journal-relative path so the entry dir segment is
        /// the on-disk N-format GUID. Accepts both D- and N-format input.</summary>
        public static string Canonicalize(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return relativePath;
            var parts = relativePath.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[0], out _) && Guid.TryParse(parts[1], out var g))
                parts[1] = g.ToString("N");
            return string.Join('/', parts);
        }

        /// <summary>True when a relative path is journal-scoped ({int}/{guid}/...).</summary>
        public static bool IsJournalRelative(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;
            var parts = relativePath.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3
                && int.TryParse(parts[0], out _)
                && Guid.TryParse(parts[1], out _);
        }
    }
}
