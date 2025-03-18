using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;
using static FileHandlingBackend.Dtos.UserDto;

namespace FileHandlingBackend.Interfaces
{
    public interface IUserInterface
    {
        string GenerateRefreshToken();
        Task<(string jwtToken, string refreshToken)> GenerateJwtToken(User user);
        Task StoreRefreshToken(User user, string refreshToken);
        Task<bool> ValidateRefreshToken(string refreshToken);
        Task<string> CreateUser(SignUpDto dto);
        Task<User?> Login(LoginDto dto);
        Task<User?> GetUserById(string id);
        Task<bool> SignOut(string userId);
        Task<bool> DeleteUser(string id);
        Task<User?> GetUserByToken(string refreshToken);
        List<User> GetAllUsers();
        Task<bool> UpdateUser(UpdateUserDto dto, string userId);
        bool ValidUpdateUserDto(UpdateUserDto dto);
        Task<List<int>?> GetUserIdsByUsername(string username);
        Task<string?> GetUsernameById(string id);
    }
}
