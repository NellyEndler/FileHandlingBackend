using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FileHandlingBackend.Services
{
    public class ModelService(FileHandlingContext context, ITagInterface tagService, IUserInterface userService) : IModelInterface
    {
        private readonly FileHandlingContext _context = context;
        private readonly ITagInterface _tagService = tagService;
        private readonly IUserInterface _userService = userService;

        public async Task<Model> CreateBasicModel(ModelDto dto, int userID)
        {
            var idTagString = _tagService.GetTagIdsByNames(dto.TagString);

            Model model = new()
            {
                Title = dto.Title,
                TagString = idTagString,
                Description = dto.Description,
                TimeStamp = DateTime.UtcNow,
                UserId = userID,
                IsPublic = dto.IsPublic,
                S3Key = dto.S3Key
            };

            _context.Models.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<ModelDto> CreateModel(ModelDto dto, int userID)
        {
            var tagString = await _tagService.CheckTag(dto.TagNames);

            Model model = new()
            {
                Title = dto.Title,
                TagString = tagString,
                Description = dto.Description,
                TimeStamp = DateTime.UtcNow,
                UserId = userID,
                S3Key = dto.S3Key
            };

            _context.Models.Add(model);
            await _context.SaveChangesAsync();

            return new ModelDto
            {
                Title = model.Title,
                TagNames = dto.TagNames,
                Description = model.Description,
                TimeStamp = model.TimeStamp
            };
        }

        public async Task<List<ModelDto>> GetModelsByUserId(string userIdString)
        {
            int.TryParse(userIdString, out int userId);
            var models = _context.Models.Where(m => m.UserId == userId).ToList();

            if (models == null)
                return null;

            var modelDtos = new List<ModelDto>();

            foreach (var model in models)
            {
                var tagNames = _tagService.GetTagNamesById(model.TagString);

                if (tagNames == null)
                    continue;

                modelDtos.Add(new ModelDto
                {
                    Id = model.Id,
                    Title = model.Title,
                    TagString = tagNames,
                    Description = model.Description,
                    TimeStamp = model.TimeStamp,
                    S3Key = model.S3Key,
                    IsPublic = model.IsPublic
                });
            }

            return modelDtos.ToList();
        }

        public async Task<bool> DeleteModel(int id)
        {
            var model = await _context.Models.Where(m => m.Id == id).FirstOrDefaultAsync();

            if (model == null)
                return false;

            _context.Models.Remove(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public Model GetModelById(int id)
        {
            return _context.Models.Where(x => x.Id == id).FirstOrDefault();
        }

        public async Task<bool> UpdateModel(UpdateModelDto dto, int id)
        {
            var model = await _context.Models.Where(m => m.Id == id).FirstOrDefaultAsync();

            if (model == null)
                return false;

            var tagString = _tagService.GetTagIdsByNames(dto.TagString);

            model.Title = dto.Title;
            model.Description = dto.Description;
            model.TagString = tagString;

            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<bool> UpdateModelIsPublic(bool isPublic, int modelId)
        {
            var model = await _context.Models.Where(m => m.Id == modelId).FirstOrDefaultAsync();

            if (model == null)
                return false;

            model.IsPublic = isPublic;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ModelDto>?> GetAllPublicModels()
        {
            var models = await _context.Models.Where(m => m.IsPublic == true).ToListAsync();

            if (models.Count == 0)
                return null;

            var modelDtos = new List<ModelDto>();

            foreach (var model in models)
            {
                var tagNames = _tagService.GetTagNamesById(model.TagString);

                if (tagNames == null)
                    continue;

                string username = await _userService.GetUsernameById(model.UserId.ToString());

                modelDtos.Add(new ModelDto
                {
                    Id = model.Id,
                    Title = model.Title,
                    TagString = tagNames,
                    Description = model.Description,
                    TimeStamp = model.TimeStamp,
                    S3Key = model.S3Key,
                    UserId = model.UserId,
                    IsPublic = model.IsPublic,
                    Username = username
                });
            }
            return modelDtos.ToList();
        }

        public async Task<List<ModelDto>?> FilterAndSearch(string searchString, string filterTags, string username)
        {
            if (String.IsNullOrEmpty(searchString) && String.IsNullOrEmpty(filterTags) && String.IsNullOrEmpty(username))
                return null;

            var models = await GetAllPublicModels();

            if (models == null || models.Count == 0)
                return null;

            var ssList = models
                    .Where(m => m.Title.ToLower().Contains(searchString.ToLower())
                    || m.Description.ToLower().Contains(searchString.ToLower()))
                    .ToList();

            var tagList = filterTags
                .Split(',')
                .Select(tag => tag
                .Trim().ToLower())
                .ToList();

            var ftList = models
                .Where(m => tagList
                .Any(tag => m.TagString.ToLower()
                .Split(",")
                .Any(t => t.Contains(tag))))
                .ToList();

            var userIdList = await _userService.GetUserIdsByUsername(username);
            var unList = models
                .Where(m => userIdList
                .Contains(m.UserId))
                .ToList();

            List<ModelDto> dtoList = [.. ssList, .. ftList, .. unList];

            return dtoList.DistinctBy(m => m.Id).ToList();
        }

        public async Task<string?> GetS3KeyFromModelId(int modelId)
        {
            var model = await _context.Models.Where(m => m.Id == modelId).FirstOrDefaultAsync();

            if (model == null)
                return null;

            return $"{model.UserId}/{model.S3Key}";
        }
    }
}
