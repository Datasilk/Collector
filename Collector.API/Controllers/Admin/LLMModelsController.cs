using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Collector.API.Models;
using Collector.Auth.Policies;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers.Admin
{
    [Route("/api/admin/openai")]
    [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
    public class LLMModelsController : ApiController
    {
        readonly ILLMModelsRepository _llmRepo;

        public LLMModelsController(ILLMModelsRepository llmRepo)
        {
            _llmRepo = llmRepo;
        }

        [HttpGet("get-all")]
        public IActionResult GetAll()
        {
            try
            {
                var models = _llmRepo.GetAll();
                return Json(new ApiResponse { success = true, data = models });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-by-id")]
        public IActionResult GetById(int id)
        {
            try
            {
                var model = _llmRepo.GetById(id);
                if (model == null) return Json(new ApiResponse { success = false, message = "Model not found" });
                return Json(new ApiResponse { success = true, data = model });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] LLMModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Model) || string.IsNullOrWhiteSpace(model.Endpoint))
                {
                    return Json(new ApiResponse { success = false, message = "Name, Model, and Endpoint are required" });
                }

                model.ExtraBody = NormalizeExtraBody(model.ExtraBody);
                var id = _llmRepo.Add(model);

                if (model.Enabled)
                {
                    Collector.AI.OpenAI.AddModel(MapToAIModel(model, id));
                }

                return Json(new ApiResponse { success = true, data = new { id } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] LLMModel model)
        {
            try
            {
                if (model.ModelId <= 0) return Json(new ApiResponse { success = false, message = "Model ID is required" });

                if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Model) || string.IsNullOrWhiteSpace(model.Endpoint))
                {
                    return Json(new ApiResponse { success = false, message = "Name, Model, and Endpoint are required" });
                }

                var existing = _llmRepo.GetById(model.ModelId);
                if (existing == null) return Json(new ApiResponse { success = false, message = "Model not found" });

                if (string.IsNullOrWhiteSpace(model.PrivateKey))
                {
                    model.PrivateKey = existing.PrivateKey;
                }

                model.ExtraBody = NormalizeExtraBody(model.ExtraBody);
                _llmRepo.Update(model);

                if (model.Enabled)
                {
                    Collector.AI.OpenAI.UpdateModel(MapToAIModel(model));
                }
                else
                {
                    Collector.AI.OpenAI.RemoveModel(model.ModelId);
                }

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("set-enabled")]
        public IActionResult SetEnabled([FromBody] SetEnabledModel model)
        {
            try
            {
                _llmRepo.SetEnabled(model.Id, model.Enabled);

                var dbModel = _llmRepo.GetById(model.Id);
                if (dbModel != null)
                {
                    if (dbModel.Enabled)
                    {
                        Collector.AI.OpenAI.UpdateModel(MapToAIModel(dbModel));
                    }
                    else
                    {
                        Collector.AI.OpenAI.RemoveModel(model.Id);
                    }
                }

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("set-preferred")]
        public IActionResult SetPreferred([FromBody] SetPreferredModel model)
        {
            try
            {
                var dbModel = _llmRepo.GetById(model.Id);
                if (dbModel == null) return Json(new ApiResponse { success = false, message = "Model not found" });

                _llmRepo.SetPreferred(model.Id, dbModel.Type);

                if (dbModel.Enabled)
                {
                    Collector.AI.OpenAI.PreferredModel = model.Id;
                }

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] DeleteLLMModelModel model)
        {
            try
            {
                _llmRepo.Delete(model.Id);
                Collector.AI.OpenAI.RemoveModel(model.Id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        private static string NormalizeExtraBody(string extraBody)
        {
            if (string.IsNullOrWhiteSpace(extraBody)) return "";
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(extraBody);
                return parsed != null ? JsonSerializer.Serialize(parsed) : "";
            }
            catch
            {
                return "";
            }
        }

        private static Collector.AI.Models.LLMModel MapToAIModel(LLMModel model, int? overrideId = null)
        {
            var id = overrideId ?? model.ModelId;
            return new Collector.AI.Models.LLMModel
            {
                ModelId = id,
                Name = model.Name,
                Model = model.Model,
                Endpoint = model.Endpoint,
                PrivateKey = model.PrivateKey,
                Type = model.Type,
                Enabled = model.Enabled,
                Preferred = model.Preferred,
                ExtraBody = string.IsNullOrWhiteSpace(model.ExtraBody)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(model.ExtraBody)
            };
        }
    }

    public class SetEnabledModel
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
    }

    public class SetPreferredModel
    {
        public int Id { get; set; }
    }

    public class DeleteLLMModelModel
    {
        public int Id { get; set; }
    }
}
