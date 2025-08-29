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
        public static string ContentPath {get;set; } = "";
        /// <summary>
        /// Gets the content of a file from the application content directory
        /// </summary>
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <returns>The file content as a string</returns>
        public static string GetFile(string relativePath)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(ContentPath)) ContentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
                var fullPath = Path.Combine(ContentPath, relativePath);
                
                // Ensure the path is valid and within the Content directory
                var normalizedFullPath = Path.GetFullPath(fullPath);
                var normalizedContentPath = Path.GetFullPath(ContentPath);
                
                if (!normalizedFullPath.StartsWith(normalizedContentPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Access to the path is denied. Path must be within the Content directory.");
                }
                
                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    return null;
                }
                
                // Read and return the file content
                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error reading file: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Asynchronously gets the content of a file from the application content directory
        /// </summary>
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <returns>The file content as a string</returns>
        public static async Task<string> GetFileAsync(string relativePath)
        {
            return await Task.Run(() => GetFile(relativePath));
        }
    
        /// <summary>
        /// Saves content to a file in the application content directory
        /// </summary>
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <param name="content">The content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static bool SaveFile(string relativePath, string content)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(ContentPath)) ContentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
                var fullPath = Path.Combine(ContentPath, relativePath);
                
                // Ensure the path is valid and within the Content directory
                var normalizedFullPath = Path.GetFullPath(fullPath);
                var normalizedContentPath = Path.GetFullPath(ContentPath);
                
                if (!normalizedFullPath.StartsWith(normalizedContentPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Access to the path is denied. Path must be within the Content directory.");
                }
                
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
                return false;
            }
        }
        
        /// <summary>
        /// Asynchronously saves content to a file in the application content directory
        /// </summary>
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <param name="content">The content to save to the file</param>
        /// <returns>True if the file was saved successfully, false otherwise</returns>
        public static async Task<bool> SaveFileAsync(string relativePath, string content)
        {
            return await Task.Run(() => SaveFile(relativePath, content));
        }

        
        /// <summary>
        /// Deletes a file from the application content directory
        /// </summary>
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <returns>True if the file was deleted successfully, false otherwise</returns>
        public static bool DeleteFile(string relativePath)
        {
            try
            {
                // Combine with Content directory and the relative path
                if(string.IsNullOrEmpty(ContentPath)) ContentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
                var fullPath = Path.Combine(ContentPath, relativePath);
                
                // Ensure the path is valid and within the Content directory
                var normalizedFullPath = Path.GetFullPath(fullPath);
                var normalizedContentPath = Path.GetFullPath(ContentPath);
                
                if (!normalizedFullPath.StartsWith(normalizedContentPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Access to the path is denied. Path must be within the Content directory.");
                }
                
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
        /// <param name="relativePath">Relative path to the file within the Content directory</param>
        /// <returns>True if the file was deleted successfully, false otherwise</returns>
        public static async Task<bool> DeleteFileAsync(string relativePath)
        {
            return await Task.Run(() => DeleteFile(relativePath));
        }
    }
}
