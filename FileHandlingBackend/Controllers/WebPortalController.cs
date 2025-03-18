using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FileHandlingBackend.Dtos;

namespace FileHandlingBackend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WebPortalController : ControllerBase
    {
        private readonly IWebPortalInterface _webPortalService;
        private readonly IS3Interface _s3Service;
        private readonly IUserInterface _userService;
        private IConfiguration _configuration;
        private readonly IModelInterface _modelService;
        public WebPortalController(IWebPortalInterface webPortalService, IUserInterface userService, IConfiguration configuration, IModelInterface modelService, IS3Interface s3Service)
        {
            _webPortalService = webPortalService;
            _userService = userService;
            _configuration = configuration;
            _modelService = modelService;
            _s3Service = s3Service;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllUserModelsAsync()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            var response = await _webPortalService.GetWebModelDtos(userId);

            if (response == null)
                return NoContent();
            else
                return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPublicModels()
        {
            var models = await _webPortalService.GetPublicWebModelDtos();

            if (models == null)
                return NotFound("No public models exist.");

            return Ok(models);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadModel(CreateFullModelDto dto)
        {
            if (dto.ModelFile == null || dto.Title.Length < 1)
                return BadRequest("Your data is invalid");

            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            int parsedNumber = int.Parse(userId);
            var response = await _webPortalService.UploadModel(dto, parsedNumber);

            if (response == null)
                return BadRequest();
            else
                return Created("Created!", response);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModel([FromBody] UpdateModelDto dto, int id)
        {
            if (id == 0)
                return BadRequest("Invalid model ID");

            var updatedModel = await _modelService.UpdateModel(dto, id);

            if (!updatedModel)
                return NotFound($"Model with ID {id} was not found");

            return Ok(updatedModel);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateModelIsPublic(bool isPublic, int modelId)
        {
            if (modelId <= 0)
                return BadRequest("Invalid model ID");

            var response = await _modelService.UpdateModelIsPublic(isPublic, modelId);

            if (!response)
                return NotFound($"Could not find model with ID {modelId}");

            return Ok(response);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
        {
            if (!_userService.ValidUpdateUserDto(dto))
                return BadRequest("Invalid data.");

            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            var resp = await _userService.UpdateUser(dto, userId);

            if (!resp)
                return BadRequest("Username or Email does already exist.");
            else
                return Ok("Username and email successfully updated.");
        }

        [HttpGet]
        public async Task<IActionResult> SearchAndFilterModels(string? searchString, string? filterTags, string? username)
        {
            var foundModels = await _modelService.FilterAndSearch(searchString, filterTags, username);

            if (foundModels == null)
                return NotFound("No models matched conditions.");

            var webModels = await _webPortalService.GetPublicWebModelDtos(foundModels);

            if (webModels == null)
                return NotFound("No models were found.");

            return Ok(webModels);
        }

    }
}
