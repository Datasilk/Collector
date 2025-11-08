using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Collector.API.Models;
using Collector.Common;
using Collector.Data.Interfaces;
using Collector.Data.Entities;

namespace Collector.API.Controllers
{
    public class FilesController : ApiController
    {
        private readonly IJournalFilesRepository _filesRepo;

        public FilesController(IJournalFilesRepository filesRepo)
        {
            _filesRepo = filesRepo;
        }

        [HttpGet("/file/{*path}")]
        public IActionResult GetFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }
            
            try
            {
                // Get the full file path
                var basePath = Files.GetPath(Files.Paths.Files);
                if (string.IsNullOrEmpty(basePath))
                {
                    return NotFound("Files path not configured");
                }
                
                var fullPath = System.IO.Path.Combine(basePath, path);
                
                // Check if file exists
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound($"File not found: {path}");
                }
                
                // Determine the correct content type from the file extension
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".txt" => "text/plain",
                    ".zip" => "application/zip",
                    ".rar" => "application/x-rar-compressed",
                    ".7z" => "application/x-7z-compressed",
                    ".json" => "application/json",
                    ".xml" => "application/xml",
                    ".csv" => "text/csv",
                    _ => "application/octet-stream"
                };
                
                var fileName = System.IO.Path.GetFileName(path);
                
                // Stream the file directly to the response
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(fileStream, contentType, fileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return NotFound($"File not found: {path}");
            }
        }

        [Authorize]
        [HttpPost("/file-upload/{*path}")]
        public async Task<IActionResult> UploadFile(string path, [FromForm] IFormFile file)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Json(new ApiResponse { success = false, message = "Path is required" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new ApiResponse { success = false, message = "File is required" });
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    
                    var success = Files.SaveFileBytes(Files.Paths.Files, path, fileBytes);
                    if (!success)
                    {
                        return Json(new ApiResponse { success = false, message = "Failed to save file" });
                    }
                }

                return Json(new ApiResponse { success = true, data = new { fileSize = file.Length } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/api/journals/files/add")]
        public IActionResult Add([FromBody] JournalFile file)
        {
            try
            {
                var id = _filesRepo.Add(file);
                return Json(new ApiResponse { success = true, data = id });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("/api/journals/files/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var file = _filesRepo.GetById(id);
                if (file == null)
                {
                    return Json(new ApiResponse { success = false, message = "File not found" });
                }
                return Json(new ApiResponse { success = true, data = file });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("/api/journals/files/entry/{entryId}/module/{moduleId}")]
        public IActionResult GetByModuleId(Guid entryId, string moduleId)
        {
            try
            {
                var file = _filesRepo.GetByModuleId(entryId, moduleId);
                return Json(new ApiResponse { success = true, data = file });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("/api/journals/files/entry/{entryId}")]
        public IActionResult GetAllByEntryId(Guid entryId)
        {
            try
            {
                var files = _filesRepo.GetAllByEntryId(entryId);
                return Json(new ApiResponse { success = true, data = files });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("/api/journals/files/journal/{journalId}")]
        public IActionResult GetAllByJournalId(int journalId)
        {
            try
            {
                var files = _filesRepo.GetAllByJournalId(journalId);
                return Json(new ApiResponse { success = true, data = files });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/api/journals/files/update")]
        public IActionResult Update([FromBody] JournalFile file)
        {
            try
            {
                _filesRepo.Update(file);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/api/journals/files/delete/{entryId}/{moduleId}")]
        public IActionResult Delete(Guid entryId, string moduleId)
        {
            try
            {
                _filesRepo.DeleteByModuleId(entryId, moduleId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/api/journals/files/delete/entry/{entryId}")]
        public IActionResult DeleteAllByEntryId(Guid entryId)
        {
            try
            {
                _filesRepo.DeleteAllByEntryId(entryId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
