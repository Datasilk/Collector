using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FeedsController : ApiController
    {
        private readonly IFeedsRepository _feedsRepository;

        public FeedsController(IFeedsRepository feedsRepository)
        {
            _feedsRepository = feedsRepository;
        }

        [HttpGet]
        public IActionResult GetList()
        {
            try
            {
                var feeds = _feedsRepository.GetList();
                return Json(new ApiResponse { success = true, data = feeds });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{feedId}")]
        public IActionResult GetInfo(int feedId)
        {
            try
            {
                var feed = _feedsRepository.GetInfo(feedId);
                if (feed == null)
                {
                    return Json(new ApiResponse { success = false, message = "Feed not found" });
                }

                return Json(new ApiResponse { success = true, data = feed });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] AddFeedModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                var feedId = _feedsRepository.Add(
                    model.DocType,
                    model.CategoryId,
                    model.Title,
                    model.Url,
                    model.Domain,
                    model.Filter,
                    model.CheckIntervals
                );

                return Json(new ApiResponse { success = true, data = feedId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("log-checked")]
        public IActionResult LogCheckedLinks([FromBody] LogCheckedLinksModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _feedsRepository.LogCheckedLinks(model.FeedId, model.Count);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("{feedId}/update-last-checked")]
        public IActionResult UpdateLastChecked(int feedId)
        {
            try
            {
                _feedsRepository.UpdateLastChecked(feedId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("with-logs")]
        public IActionResult GetListWithLogs([FromBody] FeedListWithLogsModel model)
        {
            try
            {
                var feeds = _feedsRepository.GetListWithLogs(model.Days, model.DateStart);
                return Json(new ApiResponse { success = true, data = feeds });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("categories/add")]
        public IActionResult AddCategory([FromBody] AddFeedCategoryModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _feedsRepository.AddCategory(model.Title);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = _feedsRepository.GetCategories();
                return Json(new ApiResponse { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("check")]
        public IActionResult Check()
        {
            try
            {
                var feeds = _feedsRepository.Check();
                return Json(new ApiResponse { success = true, data = feeds });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("check/{feedId}")]
        public IActionResult CheckFeed(int feedId)
        {
            try
            {
                var feeds = _feedsRepository.Check(feedId);
                return Json(new ApiResponse { success = true, data = feeds });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

    }
}
