using FileHandlingBackend.Dtos;
using FileHandlingBackend.Models;

namespace FileHandlingBackend.Interfaces
{
    public interface ITagInterface
    {
        Task<Tag?> CreateTag(TagDto dto);
        Task<string> CheckTag(List<string> tagNames);
        string GetTagIdsByNames(string tagNamesString);
        string GetTagNamesById(string tagString);
        Task<bool> DeleteTag(int id);
    }
}
