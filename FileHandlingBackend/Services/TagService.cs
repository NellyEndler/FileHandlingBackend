using FileHandlingBackend.Context;
using FileHandlingBackend.Dtos;
using FileHandlingBackend.Interfaces;
using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FileHandlingBackend.Services
{
    public class TagService(FileHandlingContext context) : ITagInterface
    {
        private readonly FileHandlingContext _context = context;

        private string GetTagStringFromList(List<Tag> tagList)
        {
            return string.Join(", ", tagList.Select(tag => tag.Id));
        }

        public async Task<Tag?> CreateTag(TagDto dto)
        {
            var existingTag = await _context.Tags.Where(t => t.Name == dto.Name).FirstOrDefaultAsync();

            if (existingTag != null)
                return null;

            Tag tag = new() { Name = dto.Name };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return new Tag { Name = tag.Name, Id = tag.Id };
        }

        public async Task<string> CheckTag(List<string> tagNames)
        {
            var tagList = new List<Tag>();

            foreach (var tagName in tagNames)
            {
                var tag = await _context.Tags.Where(t => t.Name == tagName).FirstOrDefaultAsync();

                if (tag == null)
                {
                    tag = new() { Name = tagName };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                tagList.Add(tag);
            }

            return GetTagStringFromList(tagList);
        }

        public string GetTagIdsByNames(string tagNamesString)
        {
            var tagNames = tagNamesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                    .ToList();

            var existingTags = _context.Tags
                .Where(t => tagNames.Contains(t.Name))
                    .ToList();

            var existingTagNames = existingTags.Select(t => t.Name).ToList();
            var missingTagNames = tagNames.Except(existingTagNames).ToList();

            if (missingTagNames.Any())
            {
                var newTags = missingTagNames.Select(name => new Tag { Name = name }).ToList();
                _context.Tags.AddRange(newTags);
                _context.SaveChanges();
                existingTags.AddRange(newTags);
            }

            return string.Join(",", existingTags.Select(t => t.Id));
        }

        public string GetTagNamesById(string tagString)
        {
            var tagIds = tagString
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(idStr => int.Parse(idStr))
                .ToList();

            var tags = _context.Tags
                  .Where(t => tagIds.Contains(t.Id))
                  .ToList();

            var tagNames = tags.Select(t => t.Name).ToList();
            return string.Join(",", tagNames);
        }

        public async Task<bool> DeleteTag(int id)
        {
            var tag = await _context.Tags.Where(t => t.Id == id).FirstOrDefaultAsync();

            if (tag == null)
                return false;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
