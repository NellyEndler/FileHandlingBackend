using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;
using static FileHandlingBackend.Dtos.ProjectDto;

namespace FileHandlingBackend.Services
{
    public class ProjectService(FileHandlingContext context, IS3Interface s3Service) : IProjectInterface
    {
        private readonly FileHandlingContext _context = context;
        private readonly IS3Interface _s3Service = s3Service;
    
        public async Task<string?> GetProjectFolderPath(int id)
        {
            var p = await _context.Projects
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (p == null)
                return null;

            return $"{p.OwnerId}/{p.Name}{p.OwnerId}/";
        }

        public async Task<bool> UpdateSceneIds(int projectId, Scene scene)
        {
            var project = await _context.Projects.Where(p => p.Id == projectId).FirstOrDefaultAsync();

            if (project == null)
                return false;

            if (String.IsNullOrEmpty(project.SceneIds))
                project.SceneIds = scene.Id.ToString();
            else
                project.SceneIds = project.SceneIds + "," + scene.Id;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GetSceneIdsFromProject(int ownerId, int projectId)
        {
            var projectsIds = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.SceneIds)
                .FirstOrDefaultAsync();

            if (projectsIds == null)
                return null;

            return projectsIds;
        }

        public async Task<ProjectDto?> CreateProject(CreateProjectDto dto, int userId)
        {
            if (dto == null)
                return null;

            Project p = new()
            {
                Name = dto.Name,
                OwnerId = userId,
                CollaboratorIds = dto.CollaboratorIds,
                SceneIds = dto.SceneIds,
                CreatedDate = DateTime.UtcNow,
                LastEditedById = 0,
                LastEdited = null
            };

            var folder = await _s3Service.CreateProjectFolder(p.Name, userId, "vams-assets");

            if (folder == null || folder == "creation failed")
                return null;

            _context.Projects.Add(p);
            await _context.SaveChangesAsync();

            return new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                OwnerId = p.OwnerId,
                CreatedDate = p.CreatedDate
            };
        }

        public async Task<List<ProjectDto>?> GetAllProjects(int ownerId)
        {
            var projects = await _context.Projects.Where(p => p.OwnerId == ownerId).ToListAsync();

            if (projects == null)
                return null;

            var dto = new List<ProjectDto>();

            foreach (var project in projects)
            {
                dto.Add(new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    OwnerId = project.OwnerId,
                    CreatedDate = project.CreatedDate
                });
            }
            return dto.ToList();
        }
    }
}
