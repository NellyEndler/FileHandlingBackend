using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;

namespace FileHandlingBackend.Services
{
    public class SceneService(FileHandlingContext context, IS3Interface s3Service, IProjectInterface projectService) : ISceneInterface
    {
        private readonly FileHandlingContext _context = context;
        private readonly IS3Interface _s3Service = s3Service;
        private readonly IProjectInterface _projectService = projectService;

        public async Task<Scene?> CreateScene(CreateSceneDto dto, int userId, int? projectId)
        {
            string? projectFolder = null;

            if (projectId != null && projectId.Value != 0)
                projectFolder = await _projectService.GetProjectFolderPath(projectId.Value);

            var scene = await CreateSceneAWS(dto, userId, projectFolder);

            if (scene == null)
                return null;

            if (projectId != null && projectId.Value != 0)
            {
                var projectUpdated = await _projectService.UpdateSceneIds(projectId.Value, scene);
                if (!projectUpdated)
                    return null;
            }

            return scene;
        }

        public async Task<Scene?> CreateSceneAWS(CreateSceneDto dto, int userId, string? projectFolder)
        {
            //needs validation
            if (String.IsNullOrEmpty(dto.Name))
                return null;

            if (dto == null)
                return null;

            string s3Key = _s3Service.GeneratePresentationName(projectFolder, dto.Name, userId, FileType.Json);
            var result = await _s3Service.UploadFileAnyBucket(userId.ToString(), dto.SceneFile, s3Key, _s3Service.GetAssetsBucket());

            if (result == null)
                return null;

            Scene s = new Scene()
            {
                Name = dto.Name,
                S3Key = s3Key,
                CreatedDate = DateTime.UtcNow,
                OwnerId = userId
            };

            _context.Scenes.Add(s);
            await _context.SaveChangesAsync();
            return s;
        }

        public async Task<bool> UpdateJsonSceneData(int sceneId, IFormFile file)
        {
            var s = GetScene(sceneId);
            var updatedFile = await _s3Service.UpdateScene(s.S3Key, file);

            if (updatedFile == null)
                return false;

            return true;
        }

        public async Task<List<SceneDto>?> GetAllPresentationsFromFolder(string sceneIds, string projectFolder)
        {
            List<int> idList = sceneIds.Split(',')
                           .Select(int.Parse)
                           .ToList();

            var presentations = await _context.Scenes
                .Where(s => idList.Contains(s.Id)).ToListAsync();

            if (presentations == null || presentations.Count == 0)
                return null;

            var jsonFiles = await _s3Service.GetAllJsonFilesInWebFolder(projectFolder);
            var dto = new List<SceneDto>();

            foreach (var p in presentations)
            {
                var json = jsonFiles.FirstOrDefault(j => j.Name == p.S3Key);

                dto.Add(new SceneDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    JsonUrl = json?.PresignedUrl
                });
            }

            return dto;
        }

        public async Task<List<SceneDto>?> GetAllUserPresentations(int userId)
        {
            var presentations = await _context.Scenes
                .Where(s => s.OwnerId == userId)
                .ToListAsync();

            if (presentations == null || presentations.Count == 0)
                return null;

            var jsonFiles = await _s3Service.GetAllJsonFilesInWebFolder(userId.ToString());
            var dto = new List<SceneDto>();

            foreach (var p in presentations)
            {
                var json = jsonFiles.FirstOrDefault(j => j.Name == p.S3Key);

                if (json == null)
                    continue;

                var updatedJson = await _s3Service.GetJsonFileWithUrl(json.PresignedUrl);
                var jsonFile = ConvertStringToJsonFile(updatedJson, json.Name);
                var s3ObjectDto = await _s3Service.UpdateScene(p.S3Key, jsonFile);

                dto.Add(new SceneDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    JsonUrl = s3ObjectDto.PresignedUrl  //json?.PresignedUrl
                });
            }

            return dto;
        }

        private IFormFile ConvertStringToJsonFile(string jsonString, string fileName)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "json", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/json",
                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    FileName = fileName,
                    Name = "json"  // form field name
                }.ToString()
            };
            return formFile;
        }

        private Scene? GetScene(int id)
        {
            return _context.Scenes.Where(x => x.Id == id).FirstOrDefault();
        }
    }
}
