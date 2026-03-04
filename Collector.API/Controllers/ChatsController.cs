using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Collector.API.Models;
using Collector.Data.Interfaces;
using System;
using System.Linq;

namespace Collector.API.Controllers
{
    [Route("/api/chats")]
    [Authorize]
    public class ChatsController : ApiController
    {
        private readonly IChatsRepository _chatsRepo;

        public ChatsController(IChatsRepository chatsRepo)
        {
            _chatsRepo = chatsRepo;
        }

        [HttpGet("list")]
        public IActionResult GetUserChats([FromQuery] int start = 0, [FromQuery] int length = 20)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new ApiResponse { success = false, message = "User not found" });
                }

                var chats = _chatsRepo.GetByUserIdPaginated(userId, start, length)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        created = c.Created,
                        modified = c.Modified
                    })
                    .ToList();

                return Json(new ApiResponse
                {
                    success = true,
                    data = chats
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

        [HttpGet("{chatId}/history")]
        public IActionResult GetChatHistory(Guid chatId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new ApiResponse { success = false, message = "User not found" });
                }

                // Verify chat ownership
                var chat = _chatsRepo.GetById(chatId);
                if (chat == null || chat.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Chat not found or unauthorized" });
                }

                var messages = _chatsRepo.GetMessagesByChatId(chatId)
                    .Select(m => new
                    {
                        id = m.Id,
                        role = m.Role,
                        content = m.Content,
                        model = m.Model,
                        created = m.Created
                    })
                    .ToList();

                return Json(new ApiResponse
                {
                    success = true,
                    data = new
                    {
                        chatId = chat.Id,
                        title = chat.Title,
                        messages = messages
                    }
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
