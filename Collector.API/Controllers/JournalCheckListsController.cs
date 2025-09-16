using Collector.API.Models;
using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/journal-checklists")]
    public class JournalCheckListsController : ApiController
    {
        private readonly IJournalCheckListsRepository _checkListsRepo;
        private readonly IJournalCheckListItemsRepository _checkListItemsRepo;

        public JournalCheckListsController(
            IJournalCheckListsRepository checkListsRepo,
            IJournalCheckListItemsRepository checkListItemsRepo)
        {
            _checkListsRepo = checkListsRepo;
            _checkListItemsRepo = checkListItemsRepo;
        }

        #region Checklists

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                return Json(new ApiResponse { success = true, data = checkList });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("entry/{entryId}")]
        public async Task<IActionResult> GetByEntryId(Guid entryId)
        {
            try
            {
                var checkLists = await _checkListsRepo.GetByEntryId(entryId);
                var userId = GetUserId();
                
                // Filter to only show checklists owned by the current user
                checkLists = checkLists.Where(c => c.AppUserId == userId).ToList();
                
                return Json(new ApiResponse { success = true, data = checkLists });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddCheckListModel model)
        {
            try
            {
                var userId = GetUserId();
                
                var checkList = new JournalCheckList
                {
                    AppUserId = userId,
                    EntryId = model.EntryId,
                    ThemeId = model.ThemeId,
                    Title = model.Title,
                    Description = model.Description,
                    Status = 1 // Active
                };

                var id = await _checkListsRepo.Add(checkList);
                return Json(new ApiResponse { success = true, data = id });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCheckListModel model)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(model.Id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                checkList.ThemeId = model.ThemeId;
                checkList.Title = model.Title;
                checkList.Description = model.Description;

                var success = await _checkListsRepo.Update(checkList);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-title")]
        public async Task<IActionResult> UpdateTitle([FromBody] UpdateCheckListTitleModel model)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(model.Id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListsRepo.UpdateTitle(model.Id, model.Title);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-description")]
        public async Task<IActionResult> UpdateDescription([FromBody] UpdateCheckListDescriptionModel model)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(model.Id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListsRepo.UpdateDescription(model.Id, model.Description);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(model.Id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListsRepo.UpdateStatus(model.Id, model.Status);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var checkList = await _checkListsRepo.GetById(id);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                // Verify user has access to this checklist
                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListsRepo.Delete(id);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Checklist Items

        [HttpGet("items/{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                return Json(new ApiResponse { success = true, data = item });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{checkListId}/items")]
        public async Task<IActionResult> GetItemsByCheckListId(int checkListId)
        {
            try
            {
                // Verify user has access to the checklist
                var checkList = await _checkListsRepo.GetById(checkListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var items = await _checkListItemsRepo.GetByCheckListId(checkListId);
                return Json(new ApiResponse { success = true, data = items });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("items/add")]
        public async Task<IActionResult> AddItem([FromBody] AddCheckListItemModel model)
        {
            try
            {
                // Verify user has access to the checklist
                var checkList = await _checkListsRepo.GetById(model.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var item = new JournalCheckListItem
                {
                    CheckListId = model.CheckListId,
                    Title = model.Title,
                    Icon = model.Icon,
                    Status = 0 //unchecked by default
                };

                item.Id = await _checkListItemsRepo.Add(item);
                return Json(new ApiResponse { success = true, data = item });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("items/update")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCheckListItemModel model)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(model.Id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                item.Title = model.Title;
                item.Icon = model.Icon;

                var success = await _checkListItemsRepo.Update(item);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("items/update-title")]
        public async Task<IActionResult> UpdateItemTitle([FromBody] UpdateCheckListItemTitleModel model)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(model.Id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListItemsRepo.UpdateTitle(model.Id, model.Title);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("items/update-icon")]
        public async Task<IActionResult> UpdateItemIcon([FromBody] UpdateCheckListItemIconModel model)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(model.Id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListItemsRepo.UpdateIcon(model.Id, model.Icon);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("items/update-status")]
        public async Task<IActionResult> UpdateItemStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(model.Id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListItemsRepo.UpdateStatus(model.Id, model.Status);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("items/{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            try
            {
                var item = await _checkListItemsRepo.GetById(id);
                if (item == null)
                {
                    return Json(new ApiResponse { success = false, message = "Checklist item not found" });
                }

                // Verify user has access to the parent checklist
                var checkList = await _checkListsRepo.GetById(item.CheckListId);
                if (checkList == null)
                {
                    return Json(new ApiResponse { success = false, message = "Parent checklist not found" });
                }

                var userId = GetUserId();
                if (checkList.AppUserId != userId)
                {
                    return Json(new ApiResponse { success = false, message = "Access denied" });
                }

                var success = await _checkListItemsRepo.Delete(id);
                return Json(new ApiResponse { success = success });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
