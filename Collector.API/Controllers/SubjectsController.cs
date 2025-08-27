using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Interfaces;
using Collector.Data.Entities;
using Collector.Common;
using System.Linq;

namespace Collector.API.Controllers
{
    public class SubjectsController : ApiController
    {
        private readonly ISubjectsRepository _subjectsRepository;

        public SubjectsController(ISubjectsRepository subjectsRepository)
        {
            _subjectsRepository = subjectsRepository;
        }

        [HttpGet("{id}")]
        public IActionResult GetSubject(int id)
        {
            try
            {
                var subject = _subjectsRepository.GetSubjectById(id);
                if (subject == null)
                {
                    return Json(new ApiResponse { success = false, message = "Subject not found" });
                }
                return Json(new ApiResponse { success = true, data = subject });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-title")]
        public IActionResult GetSubjectByTitle([FromQuery] SubjectByTitleRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                var subject = _subjectsRepository.GetSubjectByTitle(model.Title, model.Breadcrumb ?? "");
                if (subject == null)
                {
                    return Json(new ApiResponse { success = false, message = "Subject not found" });
                }
                return Json(new ApiResponse { success = true, data = subject });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("list")]
        public IActionResult GetSubjects([FromQuery] SubjectListRequestModel model)
        {
            try
            {
                var subjects = _subjectsRepository.GetList(model.SubjectIds ?? "", model.ParentId);
                return Json(new ApiResponse { success = true, data = subjects });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-parent/{parentId}")]
        public IActionResult GetSubjectsByParent(int parentId)
        {
            try
            {
                var subjects = _subjectsRepository.GetByParentId(parentId);
                return Json(new ApiResponse { success = true, data = subjects });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateSubject([FromBody] SubjectCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                int subjectId = _subjectsRepository.CreateSubject(
                    model.ParentId,
                    model.GrammarType,
                    model.Score,
                    model.Title,
                    model.Breadcrumb ?? ""
                );
                
                return Json(new ApiResponse { success = true, data = subjectId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPut("move")]
        public IActionResult MoveSubject([FromBody] SubjectMoveModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _subjectsRepository.Move(model.SubjectId, model.NewParentId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
