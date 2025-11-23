using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/journals/{journalId}/tags")]
    public class JournalTagsController : ApiController
    {
        private readonly IJournalsRepository _journalsRepository;
        private readonly IJournalEntriesRepository _journalEntriesRepository;
        private readonly IJournalTagsRepository _journalTagsRepository;
        private readonly IJournalEntryTagsRepository _journalEntryTagsRepository;

        public JournalTagsController(
            IJournalsRepository journalsRepository,
            IJournalEntriesRepository journalEntriesRepository,
            IJournalTagsRepository journalTagsRepository,
            IJournalEntryTagsRepository journalEntryTagsRepository)
        {
            _journalsRepository = journalsRepository;
            _journalEntriesRepository = journalEntriesRepository;
            _journalTagsRepository = journalTagsRepository;
            _journalEntryTagsRepository = journalEntryTagsRepository;
        }

        [HttpGet]
        public IActionResult GetTags(int journalId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                if (!IsUserJournal(journalId, userId))
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                var tags = _journalTagsRepository.GetByJournalId(journalId);
                return Json(new ApiResponse { success = true, data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("search")]
        public IActionResult SearchTags(int journalId, [FromBody] JournalTagSearchModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                if (!IsUserJournal(journalId, userId))
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                var tags = _journalTagsRepository.Search(journalId, request?.Search ?? string.Empty, request?.Limit ?? 10);
                return Json(new ApiResponse { success = true, data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("create-or-get")]
        public IActionResult CreateOrGetTag(int journalId, [FromBody] JournalTagCreateModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                if (!IsUserJournal(journalId, userId))
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                var tagValue = (request?.Tag ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(tagValue))
                    return Json(new ApiResponse { success = false, message = "Tag is required" });

                var existing = _journalTagsRepository.GetByJournalIdAndTag(journalId, tagValue);
                if (existing != null)
                    return Json(new ApiResponse { success = true, data = existing });

                var tag = new JournalTag
                {
                    JournalId = journalId,
                    Tag = tagValue
                };

                tag.Id = _journalTagsRepository.Add(tag);
                return Json(new ApiResponse { success = true, data = tag });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("~/api/journal-entries/{entryId}/tags")]
        public IActionResult GetEntryTags(Guid entryId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                if (!IsUserEntry(entryId, userId))
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var tags = _journalEntryTagsRepository.GetByEntryId(entryId);
                return Json(new ApiResponse { success = true, data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("~/api/journal-entries/{entryId}/tags/remove")]
        public IActionResult RemoveTagFromEntry(Guid entryId, [FromBody] JournalEntryTagModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = GetUserEntry(entryId, userId);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                if (request == null || request.TagId <= 0)
                    return Json(new ApiResponse { success = false, message = "Tag is required" });

                var tag = _journalTagsRepository.GetById(request.TagId);
                if (tag == null || tag.JournalId != entry.JournalId)
                    return Json(new ApiResponse { success = false, message = "Invalid tag" });

                _journalEntryTagsRepository.Remove(request.TagId, entryId);

                var tags = _journalEntryTagsRepository.GetByEntryId(entryId);
                return Json(new ApiResponse { success = true, data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("~/api/journal-entries/{entryId}/tags/add")]
        public IActionResult AddTagToEntry(Guid entryId, [FromBody] JournalEntryTagModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = GetUserEntry(entryId, userId);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                if (request == null || request.TagId <= 0)
                    return Json(new ApiResponse { success = false, message = "Tag is required" });

                var tag = _journalTagsRepository.GetById(request.TagId);
                if (tag == null || tag.JournalId != entry.JournalId)
                    return Json(new ApiResponse { success = false, message = "Invalid tag" });

                var existingTagIds = _journalEntryTagsRepository.GetTagIdsByEntry(entryId);
                if (!existingTagIds.Contains(request.TagId))
                {
                    _journalEntryTagsRepository.Add(new JournalEntryTag
                    {
                        TagId = request.TagId,
                        JournalEntryId = entryId
                    });
                }

                var tags = _journalEntryTagsRepository.GetByEntryId(entryId);
                return Json(new ApiResponse { success = true, data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        private bool IsUserJournal(int journalId, Guid userId)
        {
            var journal = _journalsRepository.GetById(journalId);
            return journal != null && journal.AppUserId == userId;
        }

        private bool IsUserEntry(Guid entryId, Guid userId)
        {
            return GetUserEntry(entryId, userId) != null;
        }

        private JournalEntry GetUserEntry(Guid entryId, Guid userId)
        {
            var entry = _journalEntriesRepository.GetById(entryId);
            if (entry == null)
                return null;

            var journal = _journalsRepository.GetById(entry.JournalId);
            if (journal == null || journal.AppUserId != userId)
                return null;

            return entry;
        }
    }
}
