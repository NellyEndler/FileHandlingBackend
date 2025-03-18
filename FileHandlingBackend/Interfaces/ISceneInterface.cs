using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;

namespace FileHandlingBackend.Interfaces
{
    public interface ISceneInterface
    {
        Task<Scene?> CreateScene(CreateSceneDto dto, int userId, int? projectId);
        Task<Scene?> CreateSceneAWS(CreateSceneDto dto, int userId, string? projectFolder);
        Task<bool> UpdateJsonSceneData(int sceneId, IFormFile file);
        Task<List<SceneDto>?> GetAllPresentationsFromFolder(string sceneIds, string projectFolder);
        Task<List<SceneDto>?> GetAllUserPresentations(int userId);
    }
}
