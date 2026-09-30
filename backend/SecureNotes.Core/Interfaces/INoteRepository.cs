using SecureNotes.Core.Entities;

namespace SecureNotes.Core.Interfaces
{
    public interface INoteRepository
    {
        Task<Note?> GetByIdAsync(Guid id);
        Task<IEnumerable<Note>> GetByUserIdAsync(Guid userId);
        Task<Note?> GetByShareTokenAsync(string token);
        Task<Note> CreateAsync(Note note);
        Task<Note> UpdateAsync(Note note);
        Task DeleteAsync(Guid id);
    }
}