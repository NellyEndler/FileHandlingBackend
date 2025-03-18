using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static FileHandlingBackend.Dtos.ProjectDto;

namespace FileHandlingBackend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectInterface _projectService;

        public ProjectController(IProjectInterface projectService)
        {
            _projectService = projectService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDto dto)
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);

            var p = await _projectService.CreateProject(dto, parsedNumber);

            if (p == null)
                return BadRequest("Something went wrong");
            else
                return Ok(p);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);
            var projects = await _projectService.GetAllProjects(parsedNumber);

            if (projects == null)
                NoContent();
            return Ok(projects);
        }
    }
}
