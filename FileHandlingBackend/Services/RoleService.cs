using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FileHandlingBackend.Services
{
    public class RoleService(FileHandlingContext context) : IRoleInterface
    {
        private readonly FileHandlingContext _context = context;

        public Role GetRole(int id)
        {
            return _context.Roles.Where(r => r.Id == id).FirstOrDefault();
        }

        public async Task<RoleDto?> CreateRole(RoleDto dto)
        {
            var existingRole = await _context.Roles.Where(r => r.Title == dto.Title).FirstOrDefaultAsync();

            if (existingRole != null)
                return null;

            Role role = new() { Title = dto.Title };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new RoleDto { Title = dto.Title };
        }
    }
}
