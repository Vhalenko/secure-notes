using SecureNotes.Core.Entities;

namespace SecureNotes.Core.Interfaces;

public interface INoteService
{
    Task<Note> GetByIdAsync(Guid noteId, Guid requestingUserId);
    Task<IEnumerable<Note>> GetUserNotesAsync(Guid userId);
    Task DeleteAsync(Guid noteId, Guid requestingUserId);
    Task<string> ShareAsync(Guid noteId, Guid requestingUserId);
    Task<Note> GetByShareTokenAsync(string token);
    Task<Note> CreateAsync(Guid userId, string title, string content);
    Task<Note> UpdateAsync(Guid noteId, Guid requestingUserId, string title, string content);
}