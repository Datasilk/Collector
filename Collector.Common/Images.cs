using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Collector.Common
{
    public static class Images
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _tinyPngApiKey = "";

        /// <summary>
        /// Set the TinyPNG API key for optimizing images
        /// </summary>
        /// <param name="apiKey">Your TinyPNG API key</param>
        public static void SetTinyPngApiKey(string apiKey)
        {
            _tinyPngApiKey = apiKey;
        }

        /// <summary>
        /// Resize an image to the specified dimensions
        /// </summary>
        /// <param name="imageBytes">The original image as byte array</param>
        /// <param name="width">Target width (0 to maintain aspect ratio)</param>
        /// <param name="height">Target height (0 to maintain aspect ratio)</param>
        /// <returns>Resized image as byte array</returns>
        public static byte[] ResizeImage(byte[] imageBytes, int width, int height)
        {
            using var image = Image.Load(imageBytes);
            
            // Calculate dimensions if one is set to 0 (maintain aspect ratio)
            if (width == 0 && height > 0)
            {
                width = (int)(image.Width * (height / (double)image.Height));
            }
            else if (height == 0 && width > 0)
            {
                height = (int)(image.Height * (width / (double)image.Width));
            }
            else if (width == 0 && height == 0)
            {
                // If both are 0, return original image
                return imageBytes;
            }

            image.Mutate(x => x.Resize(width, height));

            using var ms = new MemoryStream();
            image.Save(ms, image.Metadata.DecodedImageFormat ?? PngFormat.Instance);
            return ms.ToArray();
        }

        /// <summary>
        /// Resize an image file to the specified dimensions
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <param name="outputPath">Path to save the resized image</param>
        /// <param name="width">Target width (0 to maintain aspect ratio)</param>
        /// <param name="height">Target height (0 to maintain aspect ratio)</param>
        public static void ResizeImage(string filePath, string outputPath, int width, int height)
        {
            using var image = Image.Load(filePath);
            
            // Calculate dimensions if one is set to 0 (maintain aspect ratio)
            if (width == 0 && height > 0)
            {
                width = (int)(image.Width * (height / (double)image.Height));
            }
            else if (height == 0 && width > 0)
            {
                height = (int)(image.Height * (width / (double)image.Width));
            }
            else if (width == 0 && height == 0)
            {
                // If both are 0, just copy the file
                File.Copy(filePath, outputPath, true);
                return;
            }

            image.Mutate(x => x.Resize(width, height));
            image.Save(outputPath);
        }

        /// <summary>
        /// Convert any image to JPEG format
        /// </summary>
        /// <param name="imageBytes">The original image as byte array</param>
        /// <param name="quality">JPEG quality (1-100)</param>
        /// <returns>JPEG image as byte array</returns>
        public static byte[] ConvertToJpg(byte[] imageBytes, int quality = 80)
        {
            using var image = Image.Load(imageBytes);
            using var ms = new MemoryStream();
            
            var jpegEncoder = new JpegEncoder
            {
                Quality = quality
            };
            
            image.Save(ms, jpegEncoder);
            return ms.ToArray();
        }

        /// <summary>
        /// Convert an image file to JPEG format
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <param name="outputPath">Path to save the JPEG image</param>
        /// <param name="quality">JPEG quality (1-100)</param>
        public static void ConvertToJpg(string filePath, string outputPath, int quality = 80)
        {
            using var image = Image.Load(filePath);
            
            var jpegEncoder = new JpegEncoder
            {
                Quality = quality
            };
            
            image.Save(outputPath, jpegEncoder);
        }

        /// <summary>
        /// Optimize a PNG image using TinyPNG API
        /// </summary>
        /// <param name="imageBytes">The original PNG image as byte array</param>
        /// <returns>Optimized PNG image as byte array</returns>
        /// <exception cref="InvalidOperationException">Thrown when TinyPNG API key is not set</exception>
        /// <exception cref="HttpRequestException">Thrown when TinyPNG API request fails</exception>
        public static async Task<byte[]> OptimizePngAsync(byte[] imageBytes)
        {
            if (string.IsNullOrEmpty(_tinyPngApiKey))
            {
                throw new InvalidOperationException("TinyPNG API key is not set. Call SetTinyPngApiKey() first.");
            }

            // Set up authentication for TinyPNG API
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_tinyPngApiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            // Upload the image to TinyPNG
            var response = await _httpClient.PostAsync("https://api.tinify.com/shrink", new ByteArrayContent(imageBytes));
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"TinyPNG API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            // Get the URL of the optimized image
            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<TinyPngResponse>(responseJson);
            
            if (responseData?.Output?.Url == null)
            {
                throw new HttpRequestException("Invalid response from TinyPNG API");
            }

            // Download the optimized image
            var optimizedImageResponse = await _httpClient.GetAsync(responseData.Output.Url);
            return await optimizedImageResponse.Content.ReadAsByteArrayAsync();
        }

        /// <summary>
        /// Optimize a PNG image file using TinyPNG API
        /// </summary>
        /// <param name="filePath">Path to the PNG image file</param>
        /// <param name="outputPath">Path to save the optimized PNG image</param>
        /// <exception cref="InvalidOperationException">Thrown when TinyPNG API key is not set</exception>
        /// <exception cref="HttpRequestException">Thrown when TinyPNG API request fails</exception>
        public static async Task OptimizePngAsync(string filePath, string outputPath)
        {
            var imageBytes = await File.ReadAllBytesAsync(filePath);
            var optimizedBytes = await OptimizePngAsync(imageBytes);
            await File.WriteAllBytesAsync(outputPath, optimizedBytes);
        }

        /// <summary>
        /// Synchronous version of OptimizePngAsync
        /// </summary>
        /// <param name="imageBytes">The original PNG image as byte array</param>
        /// <returns>Optimized PNG image as byte array</returns>
        public static byte[] OptimizePng(byte[] imageBytes)
        {
            return OptimizePngAsync(imageBytes).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronous version of OptimizePngAsync
        /// </summary>
        /// <param name="filePath">Path to the PNG image file</param>
        /// <param name="outputPath">Path to save the optimized PNG image</param>
        public static void OptimizePng(string filePath, string outputPath)
        {
            OptimizePngAsync(filePath, outputPath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Create a thumbnail from an image with a maximum dimension
        /// </summary>
        /// <param name="imageBytes">The original image as byte array</param>
        /// <param name="maxDimension">Maximum width or height (maintains aspect ratio)</param>
        /// <param name="quality">JPEG quality for the thumbnail (1-100)</param>
        /// <returns>Thumbnail image as byte array</returns>
        public static byte[] CreateThumbnail(byte[] imageBytes, int maxDimension = 300, int quality = 80)
        {
            using var image = Image.Load(imageBytes);
            
            int width = image.Width;
            int height = image.Height;
            
            // Calculate new dimensions while maintaining aspect ratio
            if (width > height)
            {
                if (width > maxDimension)
                {
                    height = (int)(height * (maxDimension / (double)width));
                    width = maxDimension;
                }
            }
            else
            {
                if (height > maxDimension)
                {
                    width = (int)(width * (maxDimension / (double)height));
                    height = maxDimension;
                }
            }
            
            // Only resize if the image is larger than maxDimension
            if (width != image.Width || height != image.Height)
            {
                image.Mutate(x => x.Resize(width, height));
            }
            
            using var ms = new MemoryStream();
            var jpegEncoder = new JpegEncoder { Quality = quality };
            image.Save(ms, jpegEncoder);
            return ms.ToArray();
        }

        // Helper class for TinyPNG API response
        private class TinyPngResponse
        {
            public TinyPngOutput? Output { get; set; }
        }

        private class TinyPngOutput
        {
            public string? Url { get; set; }
        }
    }
}
