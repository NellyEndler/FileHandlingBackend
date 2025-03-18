using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;

namespace FileHandlingBackend.Interfaces
{
    public interface IRoleInterface
    {
        Role GetRole(int id);
        Task<RoleDto?> CreateRole(RoleDto dto);
    }
}
