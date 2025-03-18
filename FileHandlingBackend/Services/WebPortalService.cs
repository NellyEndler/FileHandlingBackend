using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;

namespace FileHandlingBackend.Services
{
    public class WebPortalService : IWebPortalInterface
    {
        private readonly IUserInterface _userService;
        private readonly IS3Interface _s3Service;
        private readonly IModelInterface _modelService;
        private readonly FileHandlingContext _context;
        private IConfiguration _configuration;
        public WebPortalService(IS3Interface s3Service, FileHandlingContext context, IConfiguration configuration, IModelInterface modelService, IUserInterface userService)
        {
            _s3Service = s3Service;
            _context = context;
            _modelService = modelService;
            _userService = userService;
        }

        public async Task<List<WebModelDto>> GetWebModelDtos(string userID)
        {
            List<WebModelDto> dtos = new List<WebModelDto>();
            var dbModels = await _modelService.GetModelsByUserId(userID);
            var models = await _s3Service.GetAllFilesForUserAnyBucket(userID, _s3Service.GetBucket());

            if (dbModels == null || models == null || dbModels.Count < 1 || models.Count < 1)
                return null;

            foreach (var model in dbModels)
            {
                string key = GetKeyWithPath(model.S3Key, userID);
                var url = models.Where(m => m.Name == key).Select(z => z.PresignedUrl).FirstOrDefault();

                WebModelDto dto = new()
                {
                    Id = model.Id,
                    Title = model.Title,
                    Description = model.Description,
                    TimeStamp = model.TimeStamp,
                    TagString = model.TagString,
                    ModelGlbUrl = url,
                    IsPublic = model.IsPublic
                };

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<List<WebModelDto>> GetPublicWebModelDtos(List<ModelDto>? filteredModels = null)
        {
            List<ModelDto> dbModels;

            if (filteredModels != null)
                dbModels = filteredModels;
            else
                dbModels = await _modelService.GetAllPublicModels();
            var modelKeys = dbModels.Select(m => GetKeyWithPath(m.S3Key, m.UserId.ToString())).ToList();
            var modelFiles = await _s3Service.GetAllFilesForPublicModels(_s3Service.GetBucket(), modelKeys);

            if (dbModels == null || modelKeys == null || modelFiles == null ||
                dbModels.Count < 1 || modelKeys.Count < 1 || modelFiles.Count < 1)
                return null;

            List<WebModelDto> dtos = new List<WebModelDto>();

            foreach (var model in dbModels)
            {
                string modelKey = GetKeyWithPath(model.S3Key, model.UserId.ToString());
                string modelUrl = modelFiles
                    .FirstOrDefault(m => m.Name.Equals(modelKey, StringComparison.OrdinalIgnoreCase))
                    ?.PresignedUrl ?? "N/A";
                string username = await _userService.GetUsernameById(model.UserId.ToString());

                WebModelDto dto = new()
                {
                    Id = model.Id,
                    Title = model.Title,
                    Description = model.Description,
                    TimeStamp = model.TimeStamp,
                    TagString = model.TagString,
                    ModelGlbUrl = modelUrl,
                    IsPublic = model.IsPublic,
                    Username = username
                };

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<int?> UploadModel(CreateFullModelDto dto, int userID)
        {
            var name = _s3Service.GenerateModelName(dto.Title, userID, FileType.Glb); //change to dynamic later
            var response = await _s3Service.UploadFileAnyBucket(userID.ToString(), dto.ModelFile, name, _s3Service.GetBucket());

            if (response == null)
                return null;

            ModelDto innerDto = new ModelDto()
            {
                Title = dto.Title,
                TagString = dto.TagString,
                Description = dto.Description,
                S3Key = name,
                UserId = userID,
            };

            var model = await _modelService.CreateBasicModel(innerDto, userID);

            if (model == null)
                return null;
            else
                return model.Id;
        }


        private static string GetKeyWithPath(string key, string userId)
        {
            return $"{userId}/{key}";
        }
    }
}
