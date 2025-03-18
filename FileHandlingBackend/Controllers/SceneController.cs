using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static FileHandlingBackend.Dtos.SceneDto;

namespace FileHandlingBackend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SceneController : ControllerBase
    {
        private readonly ISceneInterface _sceneService;
        private readonly IProjectInterface _projectService;

        public SceneController(ISceneInterface sceneService, IProjectInterface projectService)
        {
            _sceneService = sceneService;
            _projectService = projectService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateScene(CreateSceneDto dto, int? projectId)
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);
            var result = await _sceneService.CreateScene(dto, parsedNumber, projectId);

            if (result == null)
                return BadRequest("Something went wrong");

            return Ok(result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateSceneJson(int sceneId, IFormFile file)
        {
            var update = await _sceneService.UpdateJsonSceneData(sceneId, file);

            if (!update)
                return BadRequest("Something went wrong.");

            return Ok(update);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllScenesFromFolder(int projectId)
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);

            var presentationIds = await _projectService.GetSceneIdsFromProject(parsedNumber, projectId);
            var projectFolder = await _projectService.GetProjectFolderPath(projectId);
            var p = await _sceneService.GetAllPresentationsFromFolder(presentationIds, projectFolder);

            if (p == null)
                return BadRequest();

            return Ok(p);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllUserScenes()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);

            var p = await _sceneService.GetAllUserPresentations(parsedNumber);

            if (p == null)
                return BadRequest();

            return Ok(p);
        }

    }
}
