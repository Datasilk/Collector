using System.Text.Json;
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
    [Route("api/journal-snapshots")]
    public class JournalSnapshotsController : ApiController
    {
        private readonly IJournalSnapshotsRepository _snapshotsRepository;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly IJournalsRepository _journalsRepository;
        private readonly IAppUserRepository _userRepository;

        public JournalSnapshotsController(
            IJournalSnapshotsRepository snapshotsRepository,
            IJournalEntriesRepository entriesRepository,
            IJournalsRepository journalsRepository,
            IAppUserRepository userRepository)
        {
            _snapshotsRepository = snapshotsRepository;
            _entriesRepository = entriesRepository;
            _journalsRepository = journalsRepository;
            _userRepository = userRepository;
        }

        [HttpGet("entry/{entryId}")]
        public IActionResult GetSnapshotsByEntry(Guid entryId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var snapshots = _snapshotsRepository.GetAllByEntryId(entryId);
                
                // If there are snapshots, verify authorization via the first snapshot's journal
                if (snapshots.Count > 0)
                {
                    var journal = _journalsRepository.GetById(snapshots[0].JournalId);
                    if (journal == null || journal.AppUserId != userId)
                        return Json(new ApiResponse { success = false, message = "Not authorized to access these snapshots" });
                }
                else
                {
                    // If no snapshots exist, verify via the entry (if it exists)
                    var entry = _entriesRepository.GetById(entryId);
                    if (entry != null)
                    {
                        var journal = _journalsRepository.GetById(entry.JournalId);
                        if (journal == null || journal.AppUserId != userId)
                            return Json(new ApiResponse { success = false, message = "Not authorized to access this entry" });
                    }
                }

                return Json(new ApiResponse { success = true, data = snapshots });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSnapshot(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var snapshot = _snapshotsRepository.GetById(id);
                if (snapshot == null)
                    return Json(new ApiResponse { success = false, message = "Snapshot not found" });

                // Verify user has access to this snapshot via the journal
                var journal = _journalsRepository.GetById(snapshot.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to access this snapshot" });

                // Load the snapshot content — {journalId}/{entryId}/snapshots/{id}.json,
                // with the legacy root {entryId}_{id}.json as fallback.
                string? content = null;
                try
                {
                    content = Files.GetFile(Files.Paths.Journal, $"{snapshot.JournalId}/{snapshot.EntryId:N}/snapshots/{id}.json");
                }
                catch { content = null; }
                if (string.IsNullOrEmpty(content))
                {
                    try { content = Files.GetFile(Files.Paths.Journal, $"{snapshot.EntryId:N}_{id}.json"); }
                    catch { content = null; }
                }

                // Decrypt content if the snapshot is encrypted
                if (snapshot.Encrypted && !string.IsNullOrEmpty(content))
                {
                    var user = await _userRepository.FindByGuidAsync(userId);
                    if (user == null || string.IsNullOrEmpty(user.EncryptionKey))
                        return Json(new ApiResponse { success = false, message = "Cannot decrypt content without an encryption key." });

                    content = Sha256.Decrypt(content, user.EncryptionKey);
                }

                snapshot.Content = content;

                return Json(new ApiResponse { success = true, data = snapshot });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSnapshot([FromBody] CreateSnapshotModel request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var entry = _entriesRepository.GetById(request.EntryId);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to create snapshot for this entry" });

                // Create the snapshot record first to get the ID
                var snapshot = new JournalSnapshot
                {
                    EntryId = request.EntryId,
                    JournalId = entry.JournalId,
                    ChapterId = entry.ChapterId,
                    Title = entry.Title,
                    Description = entry.Description,
                    Created = entry.Created,
                    Modified = entry.Modified,
                    Status = entry.Status,
                    Encrypted = entry.Encrypted,
                    Thumbnail = entry.Thumbnail
                };

                var id = _snapshotsRepository.Add(snapshot);
                snapshot.Id = id;

                // Copy the entry document into {journalId}/{entryId}/snapshots/{id}.json —
                // new folder layout, falling back to the legacy root file.
                string? content = null;
                try { content = Files.GetFile(Files.Paths.Journal, $"{entry.JournalId}/{entry.Id:N}/entry.json"); }
                catch { content = null; }
                if (string.IsNullOrEmpty(content))
                {
                    try { content = Files.GetFile(Files.Paths.Journal, $"{entry.Id:N}.json"); }
                    catch { content = null; }
                }
                var snapshotFilePath = $"{entry.JournalId}/{entry.Id:N}/snapshots/{id}.json";
                Files.SaveFile(Files.Paths.Journal, snapshotFilePath, content);

                return Json(new ApiResponse { success = true, data = snapshot });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("delete/{id}")]
        public IActionResult DeleteSnapshot(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "User not found" });

            try
            {
                var snapshot = _snapshotsRepository.GetById(id);
                if (snapshot == null)
                    return Json(new ApiResponse { success = false, message = "Snapshot not found" });

                var entry = _entriesRepository.GetById(snapshot.EntryId);
                if (entry == null)
                    return Json(new ApiResponse { success = false, message = "Entry not found" });

                var journal = _journalsRepository.GetById(entry.JournalId);
                if (journal == null || journal.AppUserId != userId)
                    return Json(new ApiResponse { success = false, message = "Not authorized to delete this snapshot" });

                // Delete the snapshot from database
                _snapshotsRepository.Delete(id);

                // Delete the associated JSON file (new layout + legacy fallback)
                foreach (var p in new[]
                {
                    $"{snapshot.JournalId}/{entry.Id:N}/snapshots/{id}.json",
                    $"{entry.Id:N}_{id}.json"
                })
                {
                    try { Files.DeleteFile(Files.Paths.Journal, p); }
                    catch { /* file may not exist in one layout */ }
                }

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
