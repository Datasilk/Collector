using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Data.Interfaces.Users;
using Collector.Common.Encryption;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/journals")]
    public class JournalsController : ApiController
    {
        private readonly IJournalCategoriesRepository _categoriesRepository;
        private readonly IJournalsRepository _journalsRepository;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly IAppUserRepository _userRepository;

        public JournalsController(
            IJournalCategoriesRepository categoriesRepository,
            IJournalsRepository journalsRepository,
            IJournalEntriesRepository entriesRepository,
            IAppUserRepository userRepository)
        {
            _categoriesRepository = categoriesRepository;
            _journalsRepository = journalsRepository;
            _entriesRepository = entriesRepository;
            _userRepository = userRepository;
        }

        #region Journal Categories

        [HttpPost("categories")]
        public IActionResult GetCategories([FromBody] JournalCategoryFilterModel filter)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var categories = _categoriesRepository.GetAllWithJournalsByUserId(userId, filter?.Sort, filter?.Search);
                return Json(new ApiResponse { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("categories/filter")]
        public IActionResult FilterCategories([FromBody] JournalCategoryFilterModel filter)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var categories = _categoriesRepository.GetAllWithJournalsByUserId(userId, filter.Sort, filter.Search);
                return Json(new ApiResponse { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("categories/add")]
        public IActionResult AddCategory([FromBody] JournalCategory category)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            category.AppUserId = userId;
            category.Created = DateTime.UtcNow;
            category.Status = 0; // Active

            try
            {
                var id = _categoriesRepository.Add(category);
                category.Id = id;

                return Json(new ApiResponse { success = true, data = category });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("categories/rename")]
        public IActionResult RenameCategory([FromBody] JournalCategoryModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var category = _categoriesRepository.GetById(request.Id.Value);
                if (category == null || category.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Category not found or not authorized" });

                _categoriesRepository.Rename(request.Id.Value, request.Title);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("categories/change-color")]
        public IActionResult ChangeCategoryColor([FromBody] JournalCategoryModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var category = _categoriesRepository.GetById(request.Id.Value);
                if (category == null || category.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Category not found or not authorized" });

                _categoriesRepository.ChangeColor(request.Id.Value, request.Color);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("categories/archive/{id}")]
        public IActionResult ArchiveCategory(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var category = _categoriesRepository.GetById(id);
                if (category == null || category.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Category not found or not authorized" });

                _categoriesRepository.Archive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("categories/unarchive/{id}")]
        public IActionResult UnarchiveCategory(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var category = _categoriesRepository.GetById(id);
                if (category == null || category.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Category not found or not authorized" });

                _categoriesRepository.Unarchive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Journals

        [HttpGet]
        public IActionResult GetJournals()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journals = _journalsRepository.GetAllByUserId(userId);
                return Json(new ApiResponse { success = true, data = journals });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetJournal(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(id);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                return Json(new ApiResponse { success = true, data = journal });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult AddJournal([FromBody] Journal journal)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            journal.AppUserId = userId;
            journal.Created = DateTime.UtcNow;
            journal.Status = 0; // Active

            try
            {
                var id = _journalsRepository.Add(journal);
                journal.Id = id;

                return Json(new ApiResponse { success = true, data = journal });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("rename")]
        public IActionResult RenameJournal([FromBody] JournalModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(request.Id);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                _journalsRepository.Rename(request.Id, request.Title);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("change-color")]
        public IActionResult ChangeJournalColor([FromBody] JournalModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(request.Id);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                _journalsRepository.ChangeColor(request.Id, request.Color);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("archive/{id}")]
        public IActionResult ArchiveJournal(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(id);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found or not authorized" });

                _journalsRepository.Archive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("unarchive/{id}")]
        public IActionResult UnarchiveJournal(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(id);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found or not authorized" });

                _journalsRepository.Unarchive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Journal Entries

        [HttpGet("{journalId}/entries")]
        public IActionResult GetEntries(int journalId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(journalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                var entries = _entriesRepository.GetAllByJournalId(journalId);
                return Json(new ApiResponse { success = true, data = entries });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entries/{id}")]
        public IActionResult GetEntry(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to access this entry" });

                return Json(new ApiResponse { success = true, data = entry });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entries/add")]
        public IActionResult AddEntry([FromBody] JournalEntry entry)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Journal not found" });

                entry.Created = DateTime.UtcNow;

                var id = _entriesRepository.Add(entry);
                entry.Id = id;

                return Json(new ApiResponse { success = true, data = entry });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entries/rename")]
        public IActionResult RenameEntry([FromBody] JournalEntryModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.Id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to rename this entry" });

                _entriesRepository.Rename(request.Id, request.Title);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entries/update-description")]
        public IActionResult UpdateEntryDescription([FromBody] JournalEntryModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.Id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to update this entry" });

                _entriesRepository.UpdateDescription(request.Id, request.Description);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entries/archive/{id}")]
        public IActionResult ArchiveEntry(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to archive this entry" });

                _entriesRepository.Archive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entries/unarchive/{id}")]
        public IActionResult UnarchiveEntry(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to unarchive this entry" });

                _entriesRepository.Unarchive(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entries/publish/{id}")]
        public IActionResult PublishEntry(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to publish this entry" });

                _entriesRepository.Publish(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entries/modify/{id}")]
        public IActionResult ModifyEntry(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to modify this entry" });

                _entriesRepository.Modify(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entry/move")]
        public IActionResult MoveEntry([FromBody] MoveEntryModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.EntryId);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                // Check if user owns the source journal
                var sourceJournal = _journalsRepository.GetById(entry.JournalId);
                if (sourceJournal == null || sourceJournal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to move this entry" });

                // Check if user owns the target journal
                var targetJournal = _journalsRepository.GetById(request.TargetJournalId);
                if (targetJournal == null || targetJournal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to move to the target journal" });

                _entriesRepository.UpdateJournalId(request.EntryId, request.TargetJournalId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Journal Entry Content

        [HttpGet("entries/{id}/content")]
        public async Task<IActionResult> GetEntryContent(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to access this entry content" });

                var filePath = $"{id:N}.json";
                var content = Files.GetFile(Files.Paths.Journal, filePath);

                if (content == null)
                    return Json(new ApiResponse { success = false, message = "Entry content not found" });

                if (entry.Encrypted)
                {
                    var user = await _userRepository.FindByGuidAsync(userId);
                    if (user == null || string.IsNullOrEmpty(user.EncryptionKey))
                        return Json(new ApiResponse { success = false, message = "Cannot decrypt content without an encryption key." });

                    content = Sha256.Decrypt(content, user.EncryptionKey);
                }

                return Json(new ApiResponse { success = true, data = content });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entries/update-entry")]
        public async Task<IActionResult> UpdateEntryContent([FromBody] UpdateEntryContentModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.Id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to update this entry content" });

                var contentToSave = request.Content;
                if (entry.Encrypted)
                {
                    var user = await _userRepository.FindByGuidAsync(userId);
                    if (user == null || string.IsNullOrEmpty(user.EncryptionKey))
                        return Json(new ApiResponse { success = false, message = "Cannot encrypt content without an encryption key." });

                    contentToSave = Sha256.Encrypt(contentToSave, user.EncryptionKey);
                }

                var filePath = $"{request.Id:N}.json";
                var success = Files.SaveFile(Files.Paths.Journal, filePath, contentToSave);

                if (!success)
                    return Json(new ApiResponse { success = false, message = "Failed to save entry content" });

                _entriesRepository.UpdateLastModified(request.Id);

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }


        #endregion

        [HttpPost("entries/set-published")]
        public async Task<IActionResult> SetEntryPublished([FromBody] JournalEntryStateModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.Id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to update this entry" });

                _entriesRepository.SetPublished(request.Id, request.IsSet);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("entries/set-encrypted")]
        public async Task<IActionResult> SetEntryEncrypted([FromBody] JournalEntryStateModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.Id);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to update this entry" });

                var user = await _userRepository.FindByGuidAsync(userId);
                if(user == null) return Json(new ApiResponse { success = false, message = "User not found" });

                if (string.IsNullOrEmpty(user.EncryptionKey))
                {
                    user.EncryptionKey = Sha256.GenerateKey();
                    user.EncryptionType = "AES";
                    _userRepository.UpdateEncryption(user.Id.Value, user.EncryptionKey, user.EncryptionType);
                }

                var filePath = $"{request.Id:N}.json";
                var content = Files.GetFile(Files.Paths.Journal, filePath);

                if (request.IsSet)
                {
                    // Encrypt the file
                    var encryptedContent = Sha256.Encrypt(content, user.EncryptionKey);
                    Files.SaveFile(Files.Paths.Journal, filePath, encryptedContent);
                }
                else
                {
                    // Decrypt the file
                    var decryptedContent = Sha256.Decrypt(content, user.EncryptionKey);
                    Files.SaveFile(Files.Paths.Journal, filePath, decryptedContent);
                }

                _entriesRepository.SetEncrypted(request.Id, request.IsSet);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
