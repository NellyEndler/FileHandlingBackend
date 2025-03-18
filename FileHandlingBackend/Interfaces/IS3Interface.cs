using Amazon.S3.Model;
using FileHandlingBackend.Dtos;

namespace FileHandlingBackend.Interfaces
{
    public interface IS3Interface
    {
        string GeneratePresentationName(string? projectFolder, string Title, int userID, FileType type);
        Task<PutObjectResponse> UploadFileAnyBucket(string userID, IFormFile file, string fileName, string bucket);
        string GetBucket();
        string GetAssetsBucket();
        Task<S3ObjectDto?> UpdateScene(string s3Key, IFormFile file);
        Task<List<S3ObjectDto>> GetAllJsonFilesInWebFolder(string projectFolder);
        Task<string> GetJsonFileWithUrl(string jsonUrl);
        Task<List<S3ObjectDto>> GetAllFilesForUserAnyBucket(string userID, string bucket);
        Task<List<S3ObjectDto>> GetAllFilesForPublicModels(string bucket, List<string> s3Keys);
        string GenerateModelName(string Title, int userID, FileType type);
        Task<string> CreateProjectFolder(string projectName, int userID, string bucket);
    }
}
