using System;
using Collector.API.Models;
using Collector.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers
{
    /// <summary>
    /// API controller for language-related operations
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class LanguagesController : ApiController
    {
        /// <summary>
        /// Get all available language codes and their names
        /// </summary>
        /// <returns>Dictionary of language codes and names</returns>
        [HttpGet]
        public IActionResult GetAllLanguages()
        {
            try
            {
                return Json(new ApiResponse
                {
                    success = true,
                    data = LanguageCodes.Codes
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get language name by code
        /// </summary>
        /// <param name="code">ISO 639-1 language code</param>
        /// <returns>Language name for the specified code</returns>
        [HttpGet("{code}")]
        public IActionResult GetLanguageByCode(string code)
        {
            try
            {
                string languageName = LanguageCodes.GetLanguageName(code);
                
                return Json(new ApiResponse
                {
                    success = true,
                    data = new { code, name = languageName }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Validate if a language code is valid
        /// </summary>
        /// <param name="code">ISO 639-1 language code to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        [HttpGet("validate/{code}")]
        public IActionResult ValidateLanguageCode(string code)
        {
            try
            {
                bool isValid = LanguageCodes.IsValidCode(code);
                
                return Json(new ApiResponse
                {
                    success = true,
                    data = new { code, isValid }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
