using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Collector.Common
{
    /// <summary>
    /// Provides file system operations for the application
    /// </summary>
    public static class Files
    {
        public static string ArticlesPath {get;set; } = "";
        public static string JournalPath {get;set; } = "";

        public enum Paths
        {
            Articles,
            Journal
        }


        public static string GetPath(Paths path)
        {
            switch (path)
            {
                case Paths.Articles:
                    return ArticlesPath;
                case Paths.Journal:
                    return JournalPath;
                default:
                    return "";
            }
        }

        /// <summary>
        /// Resolves a stored relative path to a full path under the Journal
        /// root. Journal-scoped paths ({journalId}/{entryId}/...) are
        /// canonicalized so entry dirs match the on-disk N-format GUID.
        /// </summary>
        public static string ResolveExisting(string relativePath)
        {
            var rel = relativePath.Replace('\\', '/').TrimStart('/');
            if (JournalPaths.IsJournalRelative(rel))
            {
                // Entry dirs on disk are N-format GUIDs; stored paths may be either.
                var journalCandidate = Path.Combine(JournalPath, JournalPaths.Canonicalize(rel));
                if (File.Exists(journalCandidate)) return journalCandidate;
            }
            return Path.Combine(JournalPath, JournalPaths.Canonicalize(rel));
        }

        /// <summary>
        /// Gets the content of a file from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>The file content as a string</returns>
        public static string GetFile(Paths path, string relativePath)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("File not found");
                }
                
                // Read and return the file content
                return File.ReadAllText(fullPath);
            }
            catch (FileNotFoundException)
            {
                // Expected miss during speculative probes (legacy-vs-new
                // layout, optional research.json) — the exception itself
                // carries the signal; don't spam the console.
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error reading file: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the content of a file as a byte array from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>The file content as a byte array</returns>
        public static byte[] GetFileBytes(Paths path, string relativePath)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("File not found");
                }
                
                // Read and return the file content as bytes
                return File.ReadAllBytes(fullPath);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error reading file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously gets the content of a file from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>The file content as a string</returns>
        public static async Task<string> GetFileAsync(Paths path, string relativePath)
        {
            return await Task.Run(() => GetFile(path, relativePath));
        }

        /// <summary>
        /// Asynchronously gets the content of a file as a byte array from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>The file content as a byte array</returns>
        public static async Task<byte[]> GetFileBytesAsync(Paths path, string relativePath)
        {
            return await Task.Run(() => GetFileBytes(path, relativePath));
        }
    
        /// <summary>
        /// Saves content to a file in the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <param name="content">The content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static bool SaveFile(Paths path, string relativePath, string content)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write the content to the file
                File.WriteAllText(fullPath, content);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error saving file: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Asynchronously saves content to a file in the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <param name="content">The content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static async Task<bool> SaveFileAsync(Paths path, string relativePath, string content)
        {
            return await Task.Run(() => SaveFile(path, relativePath, content));
        }
        
        /// <summary>
        /// Saves binary content to a file in the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <param name="content">The binary content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static bool SaveFileBytes(Paths path, string relativePath, byte[] content)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write the binary content to the file
                File.WriteAllBytes(fullPath, content);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error saving file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Asynchronously saves binary content to a file in the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <param name="content">The binary content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static async Task<bool> SaveFileBytesAsync(Paths path, string relativePath, byte[] content)
        {
            return await Task.Run(() => SaveFileBytes(path, relativePath, content));
        }

        /// <summary>
        /// Asynchronously saves a stream to a file in the application content directory
        /// This method is optimized for large files as it streams directly to disk without loading into memory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <param name="stream">The stream to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static async Task<bool> SaveFileStreamAsync(Paths path, string relativePath, Stream stream)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Stream the content directly to the file
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                {
                    await stream.CopyToAsync(fileStream);
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error saving file stream: {ex.Message}");
                return false;
            }
        }

        
        /// <summary>
        /// Deletes a file from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>True if the file was deleted successfully, false otherwise</returns>
        public static bool DeleteFile(Paths path, string relativePath)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(GetPath(path))) throw new Exception("Path not found");
                var fullPath = Path.Combine(GetPath(path), relativePath);
                
                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    return false;
                }
                
                // Delete the file
                File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously deletes a file from the application content directory
        /// </summary>
        /// <param name="path">the sub folder within the Content directory to use</param>
        /// <param name="relativePath">Relative path to the file within the Content sub folder</param>
        /// <returns>True if the file was deleted successfully, false otherwise</returns>
        public static async Task<bool> DeleteFileAsync(Paths path, string relativePath)
        {
            return await Task.Run(() => DeleteFile(path, relativePath));
        }
    }
}
