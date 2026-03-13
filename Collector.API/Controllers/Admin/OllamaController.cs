using Collector.API.Models;
using Collector.Auth.Policies;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

namespace Collector.API.Controllers.Admin
{
    [Route("/api/admin/ollama")]
    [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
    public class OllamaController : ApiController
    {
        readonly IOllamaModelsRepository _ollamaRepo;
        readonly OllamaApiClient _ollama;

        public OllamaController(
            IOllamaModelsRepository ollamaRepo,
            OllamaApiClient ollama
        )
        {
            _ollamaRepo = ollamaRepo;
            _ollama = ollama;
        }

        [HttpGet("get-all")]
        public IActionResult GetAll()
        {
            try
            {
                var models = _ollamaRepo.GetAll();
                return Json(new ApiResponse { success = true, data = models });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-active")]
        public IActionResult GetActive()
        {
            try
            {
                var model = _ollamaRepo.GetActive();
                return Json(new ApiResponse { success = true, data = model });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] OllamaModelAddModel model)
        {
            try
            {
                var ollamaModel = new OllamaModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Notes = model.Notes,
                    Status = 0
                };
                var id = _ollamaRepo.Add(ollamaModel);
                return Json(new ApiResponse { success = true, data = new { id } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] OllamaModel model)
        {
            try
            {
                _ollamaRepo.Update(model);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("set-active")]
        public async Task<IActionResult> SetActive([FromBody] SetActiveModelModel model)
        {
            try
            {
                // First, check if model exists in Ollama
                var models = await _ollama.ListLocalModelsAsync();
                var modelExists = models.Any(m => m.Name == model.Id || m.Name.StartsWith(model.Id + ":"));

                // If model doesn't exist, pull it
                if (!modelExists)
                {
                    var pullRequest = new OllamaSharp.Models.PullModelRequest { Model = model.Id };
                    await foreach (var status in _ollama.PullModelAsync(pullRequest))
                    {
                        // Model is being pulled, status updates are happening
                        if (status?.Status != null)
                        {
                            // Could send progress updates via SignalR here if needed
                        }
                    }
                }

                // Set as active in database
                _ollamaRepo.SetActive(model.Id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] DeleteModelModel model)
        {
            try
            {
                _ollamaRepo.Delete(model.Id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("list-available")]
        public async Task<IActionResult> ListAvailable()
        {
            try
            {
                var models = await _ollama.ListLocalModelsAsync();
                var modelList = models.Select(m => new
                {
                    name = m.Name,
                    size = m.Size,
                    modifiedAt = m.ModifiedAt
                });
                return Json(new ApiResponse { success = true, data = modelList });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }

    public class OllamaModelAddModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
    }

    public class SetActiveModelModel
    {
        public string Id { get; set; }
    }

    public class DeleteModelModel
    {
        public string Id { get; set; }
    }
}
