using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;

namespace FileHandlingBackend.Interfaces
{
    public interface IModelInterface
    {
        Task<ModelDto> CreateModel(ModelDto dto, int userID);
        Task<List<ModelDto>> GetModelsByUserId(string userId);
        Task<bool> DeleteModel(int id);
        Task<bool> UpdateModel(UpdateModelDto dto, int id);
        Model GetModelById(int id);
        Task<Model> CreateBasicModel(ModelDto dto, int userID);
        Task<bool> UpdateModelIsPublic(bool isPublic, int modelId);
        Task<List<ModelDto>?> GetAllPublicModels();
        Task<List<ModelDto>?> FilterAndSearch(string searchString, string filterTags, string username);
        Task<string?> GetS3KeyFromModelId(int modelId);
    }
}
