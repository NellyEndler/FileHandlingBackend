using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static FileHandlingBackend.Dtos.UserDto;

namespace FileHandlingBackend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserInterface _userService;

        public UserController(IUserInterface userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> LoginUser([FromBody] LoginDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var user = await _userService.Login(dto);

            if (user == null)
                return Unauthorized("Invalid login attempt");

            var (token, refreshToken) = await _userService.GenerateJwtToken(user);
            return Ok(new
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Message = "Login successfull."
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] SignUpDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var user = await _userService.CreateUser(dto);

            if (user == "username")
                return BadRequest("Username already exist.");
            if (user == "email")
                return BadRequest("Email already exist.");

            return Created("User successfully created!", user);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto dto)
        {
            if (dto == null || String.IsNullOrEmpty(dto.RefreshToken))
                return BadRequest("Invalid data");

            var user = await _userService.GetUserByToken(dto.RefreshToken);

            if (user == null)
                return Unauthorized("Invalid user.");

            var isValid = await _userService.ValidateRefreshToken(dto.RefreshToken);

            if (!isValid)
                return Unauthorized();

            var (newAccessToken, newRefreshToken) = await _userService.GenerateJwtToken(user);
            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LogOutUser()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            var result = await _userService.SignOut(userId);

            if (!result)
                return Unauthorized("No refresh token found.");

            return Ok("Signed out successfully");
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

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteUser()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (String.IsNullOrEmpty(userId))
                return BadRequest("No User ID found.");

            var delete = await _userService.DeleteUser(userId);

            if (!delete)
                return NotFound($"$Could not find user with ID {userId} to delete.");

            return Ok($"User with ID {userId} successfully deleted.");
        }
    }
}

