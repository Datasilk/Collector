using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Interfaces;
using Collector.Data.Enums;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Collector.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DownloadsController : ApiController
    {
        private readonly IDownloadsRepository _downloadsRepository;

        public DownloadsController(IDownloadsRepository downloadsRepository)
        {
            _downloadsRepository = downloadsRepository;
        }

        /// <summary>
        /// Get the count of items in the download queue
        /// </summary>
        [HttpGet("count")]
        public IActionResult GetCount()
        {
            try
            {
                var count = _downloadsRepository.Count();
                return Json(new ApiResponse { success = true, data = count });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Check the download queue for the next item to process
        /// </summary>
        [HttpPost("check")]
        public IActionResult CheckQueue([FromBody] CheckQueueModel model)
        {
            try
            {
                var queueItem = _downloadsRepository.CheckQueue(
                    model.FeedId, 
                    model.Domain, 
                    model.DomainDelay, 
                    model.Sort
                );

                if (queueItem == null)
                {
                    return Json(new ApiResponse { success = false, message = "No queue items found" });
                }

                return Json(new ApiResponse { success = true, data = queueItem });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add a single item to the download queue
        /// </summary>
        [HttpPost("add")]
        public IActionResult AddQueueItem([FromBody] AddQueueItemModel model)
        {
            try
            {
                var queueId = _downloadsRepository.AddQueueItem(
                    model.Url,
                    model.Domain,
                    model.ParentId,
                    model.FeedId
                );

                return Json(new ApiResponse { success = true, data = queueId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add multiple items to the download queue
        /// </summary>
        [HttpPost("add-bulk")]
        public IActionResult AddQueueItems([FromBody] AddQueueItemsModel model)
        {
            try
            {
                var count = _downloadsRepository.AddQueueItems(
                    model.Urls,
                    model.Domain,
                    model.ParentId,
                    model.FeedId
                );

                return Json(new ApiResponse { success = true, data = count });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update the status of a queue item
        /// </summary>
        [HttpPut("update-status")]
        public IActionResult UpdateQueueItem([FromBody] UpdateQueueItemModel model)
        {
            try
            {
                _downloadsRepository.UpdateQueueItem(model.QueueId, model.Status);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update the URL of a queue item
        /// </summary>
        [HttpPut("update-url")]
        public IActionResult UpdateUrl([FromBody] UpdateUrlModel model)
        {
            try
            {
                _downloadsRepository.UpdateUrl(model.QueueId, model.Url, model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a queue item
        /// </summary>
        [HttpDelete("{queueId}")]
        public IActionResult Delete(long queueId)
        {
            try
            {
                _downloadsRepository.Delete(queueId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Move a queue item to the downloads table
        /// </summary>
        [HttpPost("{queueId}/move")]
        public IActionResult Move(long queueId)
        {
            try
            {
                _downloadsRepository.Move(queueId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Archive a queue item
        /// </summary>
        [HttpPost("{queueId}/archive")]
        public IActionResult Archive(long queueId)
        {
            try
            {
                _downloadsRepository.Archive(queueId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Move all archived queue items to the downloads table
        /// </summary>
        [HttpPost("move-archived")]
        public IActionResult MoveArchived()
        {
            try
            {
                _downloadsRepository.MoveArchived();
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
