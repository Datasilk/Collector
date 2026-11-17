using System;
using System.Collections.Generic;
using Collector.API.Models;
using Collector.Auth.Policies;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers.Admin
{
    [Route("/api/admin/search-engines")]
    [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
    public class SearchEnginesController : ApiController
    {
        readonly ISearchEnginesRepository _enginesRepo;

        public SearchEnginesController(ISearchEnginesRepository enginesRepo)
        {
            _enginesRepo = enginesRepo;
        }

        [HttpGet("get-all")]
        public IActionResult GetAll()
        {
            try
            {
                var engines = _enginesRepo.GetAll();
                return Json(new ApiResponse { success = true, data = engines });
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
                var engine = _enginesRepo.GetById(id);
                if (engine == null) return Json(new ApiResponse { success = false, message = "Search engine not found" });
                return Json(new ApiResponse { success = true, data = engine });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] SearchEngine engine)
        {
            try
            {
                var validationError = ValidateEngine(engine);
                if (validationError != null) return Json(new ApiResponse { success = false, message = validationError });

                var id = _enginesRepo.Add(engine);
                return Json(new ApiResponse { success = true, data = new { id } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] SearchEngine engine)
        {
            try
            {
                if (engine.SearchEngineId <= 0) return Json(new ApiResponse { success = false, message = "Search engine ID is required" });

                var validationError = ValidateEngine(engine);
                if (validationError != null) return Json(new ApiResponse { success = false, message = validationError });

                var existing = _enginesRepo.GetById(engine.SearchEngineId);
                if (existing == null) return Json(new ApiResponse { success = false, message = "Search engine not found" });

                _enginesRepo.Update(engine);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("set-enabled")]
        public IActionResult SetEnabled([FromBody] SetSearchEngineEnabledModel model)
        {
            try
            {
                _enginesRepo.SetEnabled(model.Id, model.Enabled);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("reorder")]
        public IActionResult Reorder([FromBody] ReorderSearchEnginesModel model)
        {
            try
            {
                if (model.Ids == null || model.Ids.Count == 0)
                    return Json(new ApiResponse { success = false, message = "Ordered search engine IDs are required" });

                _enginesRepo.UpdateSort(model.Ids);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] DeleteSearchEngineModel model)
        {
            try
            {
                _enginesRepo.Delete(model.Id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        private static string ValidateEngine(SearchEngine engine)
        {
            if (string.IsNullOrWhiteSpace(engine.Name)) return "Name is required";
            if (string.IsNullOrWhiteSpace(engine.Url)) return "Url is required";
            var isPost = (engine.Method ?? "GET").Equals("POST", StringComparison.OrdinalIgnoreCase);
            var hasPlaceholder = engine.Url.Contains("{search}")
                || (isPost && (engine.RequestBodyVars ?? "").Contains("{search}"));
            if (!hasPlaceholder) return "The {search} placeholder is required (in the Url, or body vars for POST engines)";
            if (!Uri.TryCreate(engine.Url.Replace("{search}", "test"), UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
                return "Url must be a valid http(s) URL";
            engine.Name = engine.Name.Trim();
            engine.Url = engine.Url.Trim();
            engine.Type = string.IsNullOrWhiteSpace(engine.Type) ? "web" : engine.Type.Trim();
            engine.Method = isPost ? "POST" : "GET";
            return null;
        }
    }

    public class SetSearchEngineEnabledModel
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
    }

    public class ReorderSearchEnginesModel
    {
        public List<int> Ids { get; set; }
    }

    public class DeleteSearchEngineModel
    {
        public int Id { get; set; }
    }
}
