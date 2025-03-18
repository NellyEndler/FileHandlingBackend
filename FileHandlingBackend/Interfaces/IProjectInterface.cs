using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;

namespace FileHandlingBackend.Interfaces
{
    public interface IProjectInterface
    {
        Task<string?> GetProjectFolderPath(int id);
        Task<bool> UpdateSceneIds(int projectId, Scene scene);
        Task<string?> GetSceneIdsFromProject(int ownerId, int projectId);
        Task<ProjectDto?> CreateProject(CreateProjectDto dto, int userId);
        Task<List<ProjectDto>?> GetAllProjects(int ownerId);
    }
}
