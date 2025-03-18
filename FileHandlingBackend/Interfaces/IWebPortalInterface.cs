using FileHandlingBackend.Dtos;

namespace FileHandlingBackend.Interfaces
{
    public interface IWebPortalInterface
    {
        Task<List<WebModelDto>> GetWebModelDtos(string userID);
        Task<List<WebModelDto>> GetPublicWebModelDtos(List<ModelDto>? filteredModels = null);
        Task<int?> UploadModel(CreateFullModelDto dto, int userID);
    }
}
