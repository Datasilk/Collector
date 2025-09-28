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
        public static string ImagesPath {get;set; } = "";
        public static string FilesPath {get;set; } = "";

        public enum Paths
        {
            Articles,
            Journal,
            Images,
            Files
        }


        public static string GetPath(Paths path)
        {
            switch (path)
            {
                case Paths.Articles:
                    return ArticlesPath;
                case Paths.Journal:
                    return JournalPath;
                case Paths.Images:
                    return ImagesPath;
                case Paths.Files:
                    return FilesPath;
                default:
                    return "";
            }
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
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error reading file: {ex.Message}");
                throw ex;
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
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error reading file: {ex.Message}");
                throw ex;
            }
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
                return false;
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
