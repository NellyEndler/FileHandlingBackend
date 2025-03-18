using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.IdentityModel.Tokens;
using static FileHandlingBackend.Dtos.UserDto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FileHandlingBackend.Context;
using FileHandlingBackend.Cryptography;
using Microsoft.EntityFrameworkCore;
using FileHandlingBackend.Dtos;

namespace FileHandlingBackend.Services
{
    public class UserService: IUserInterface
    {
        private readonly FileHandlingContext _context;
        private readonly IHasher _hasher;
        private readonly IConfiguration _configuration;

        public UserService(FileHandlingContext context, IConfiguration configuration, IHasher hasher)
        {
            _context = context;
            _configuration = configuration;
            _hasher = hasher;
        }

        public List<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public async Task<string> CreateUser(SignUpDto dto)
        {
            var existingUserName = DoesUserExist(dto.Username, true);
            var existingUserEmail = DoesUserExist(dto.Email, false);

            if (existingUserName)
                return "username";
            if (existingUserEmail)
                return "email";

            int role = await _context.Roles.Where(r => r.Title == "User").Select(u => u.Id).FirstOrDefaultAsync();
            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                RoleId = role,
                JoinedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginCreds = new LoginCredential
            {
                UserId = user.Id,
                UserName = user.UserName,
                HashedPassword = _hasher.CreateHash(dto.Password)
            };

            _context.LoginCredentials.Add(loginCreds);
            await _context.SaveChangesAsync();
            return $"{user.Id}";
        }

        public async Task<User?> Login(LoginDto dto)
        {
            var user = await _context.Users.Where(u => u.UserName == dto.Username).FirstOrDefaultAsync();

            if (user == null)
                return null;

            var loginCreds = await _context.LoginCredentials.Where(l => l.UserId == user.Id).FirstOrDefaultAsync();

            var hashedPassword = await _context.LoginCredentials.Where(l => l.UserId == user.Id).FirstOrDefaultAsync();
            if (hashedPassword == null)
                return null;

            if (!_hasher.ValidatePassword(dto.Password, hashedPassword.HashedPassword))
                return null;

            loginCreds.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> SignOut(string userId)
        {
            var refreshToken = await _context.Tokens.Where(t => t.UserId.ToString() == userId).FirstOrDefaultAsync();

            if (refreshToken == null)
                return false;

            refreshToken.Expires = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public bool DoesUserExist(string? value, bool name)
        {
            User response;
            var users = _context.Users.ToList();

            if (name)
                response = users.Where(u => u.UserName.ToLower() == value.ToLower()).FirstOrDefault();
            else
                response = users.Where(u => u.Email.ToLower() == value.ToLower()).FirstOrDefault();

            if (response != null)
                return true;
            else
                return false;
        }

        public async Task<User?> GetUserById(string id)
        {
            var user = await _context.Users
                .Where(u => u.Id
                .ToString() == id)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            return user;
        }

        public async Task<string?> GetUsernameById(string id)
        {
            var username = await _context.Users
                .Where(u => u.Id
                .ToString() == id)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            if (username == null)
                return null;

            return username;
        }

        public async Task<List<int>?> GetUserIdsByUsername(string username)
        {
            var users = await _context.Users.ToListAsync();
            var userIds = users
                .Where(u => u.UserName
                .ToLower()
                .Contains(username
                .ToLower()))
                .Select(u => u.Id)
                .ToList();

            return userIds;
        }

        public async Task<User?> GetUserByToken(string refreshToken)
        {
            var user = await _context.Users
                .Where(u => _context.Tokens
                .Any(t => t.UserId == u.Id && t.StringToken == refreshToken && t.Expires > DateTime.UtcNow))
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            return user;
        }

        public async Task<bool> DeleteUser(string id)
        {
            var user = await _context.Users
                .Where(u => u.Id
                .ToString() == id)
                .FirstOrDefaultAsync();
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUser(UpdateUserDto dto, string userId)
        {
            var user = await _context.Users
                .Where(u => u.Id
                .ToString() == userId && u.UserName == dto.OldUsername)
                .FirstOrDefaultAsync();

            if (user == null)
                return false;

            var existingUserName = DoesUserExist(dto.NewUsername, true);
            var existingUserEmail = DoesUserExist(dto.NewEmail, false);

            if (dto.NewUsername != user.UserName && existingUserName)
                return false;
            if (dto.NewEmail != user.Email && existingUserEmail)
                return false;

            user.UserName = dto.NewUsername;
            user.Email = dto.NewEmail;
            await _context.SaveChangesAsync();
            return true;
        }

        public bool ValidUpdateUserDto(UpdateUserDto dto)
        {
            if (dto == null || String.IsNullOrEmpty(dto.OldUsername) || String.IsNullOrEmpty(dto.OldEmail)
                || String.IsNullOrEmpty(dto.NewUsername) || String.IsNullOrEmpty(dto.NewEmail))
                return false;
            else
                return true;
        }

        public async Task<(string jwtToken, string refreshToken)> GenerateJwtToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var role = await _context.Roles.FindAsync(user.RoleId);
            if (role == null)
                throw new Exception("Användarrollen hittades inte.");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role.Title)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();
            await StoreRefreshToken(user, refreshToken);

            return (jwtToken, refreshToken); ;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        public async Task StoreRefreshToken(User user, string refreshToken)
        {
            var existingToken = await _context.Tokens
                .Where(x => x.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (existingToken != null)
            {
                existingToken.StringToken = refreshToken;
                existingToken.Expires = DateTime.UtcNow.AddMinutes(60);
                await _context.SaveChangesAsync();
            }
            else
            {
                var newToken = new Token
                {
                    StringToken = refreshToken,
                    UserId = user.Id,
                    Expires = DateTime.UtcNow.AddMinutes(60),
                };

                _context.Tokens.Add(newToken);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateRefreshToken(string refreshToken)
        {
            var storedToken = await _context.Tokens
                .Where(t => t.StringToken == refreshToken)
                .FirstOrDefaultAsync();

            if (storedToken != null)
            {
                if (storedToken.Expires < DateTime.UtcNow)
                    return false;
                if (storedToken.StringToken == refreshToken)
                    return true;
            }
            return false;
        }
    }
}
