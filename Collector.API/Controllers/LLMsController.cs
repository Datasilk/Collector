using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Collector.API.Models;
using Collector.Common;

namespace Collector.API.Controllers
{
    [Route("/api/llms")]
    [Authorize]
    public class LLMsController : ApiController
    {
        public LLMsController()
        {
        }

        [HttpGet("available")]
        public IActionResult GetAvailableLLMs()
        {
            try
            {
                var llms = LLMs.Available.Select(kvp => new
                {
                    key = kvp.Key.ToString(),
                    model = kvp.Value.Model,
                    endpoint = kvp.Value.Endpoint,
                    hasKey = !string.IsNullOrEmpty(kvp.Value.PrivateKey)
                }).Where(llm => llm.hasKey).ToList();

                return Json(new ApiResponse
                {
                    success = true,
                    data = llms
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
