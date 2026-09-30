using SecureNotes.Core.Entities;
using SecureNotes.Core.Enums;
using SecureNotes.Core.Interfaces;

namespace SecureNotes.Core.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEncryptionService _encryptionService;

    public NoteService(
        INoteRepository noteRepository,
        IAuditLogRepository auditLogRepository,
        IEncryptionService encryptionService)
    {
        _noteRepository = noteRepository;
        _auditLogRepository = auditLogRepository;
        _encryptionService = encryptionService;
    }

    public async Task<Note> CreateAsync(Guid userId, string title, string content)
    {
        var (cipherText, iv) = _encryptionService.Encrypt(content);

        var note = new Note
        {
            Title = title,
            EncryptedContent = cipherText,
            IV = iv,
            UserId = userId,
        };

        var created = await _noteRepository.CreateAsync(note);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "CREATE_NOTE",
            EntityType = "Note",
            EntityId = created.Id,
            UserId = userId,
            Success = true,
        });

        return created;
    }

    public async Task<Note> GetByIdAsync(Guid noteId, Guid requestingUserId)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);

        if (note is null)
        {
            await LogFailedAccessAsync(noteId, requestingUserId, "GET_NOTE", "Note not found");
            throw new KeyNotFoundException($"Note {noteId} not found.");
        }

        if (note.UserId != requestingUserId)
        {
            await LogFailedAccessAsync(noteId, requestingUserId, "GET_NOTE", "Access denied");
            throw new UnauthorizedAccessException("Access denied.");
        }

        note.EncryptedContent = _encryptionService.Decrypt(note.EncryptedContent, note.IV);
        return note;
    }

    public async Task<IEnumerable<Note>> GetUserNotesAsync(Guid userId)
    {
        return await _noteRepository.GetByUserIdAsync(userId);
    }

    public async Task<Note> UpdateAsync(Guid noteId, Guid requestingUserId, string title, string content)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);

        if (note is null)
            throw new KeyNotFoundException($"Note {noteId} not found.");

        if (note.UserId != requestingUserId)
        {
            await LogFailedAccessAsync(noteId, requestingUserId, "UPDATE_NOTE", "Access denied");
            throw new UnauthorizedAccessException("Access denied.");
        }

        var (cipherText, iv) = _encryptionService.Encrypt(content);
        note.Title = title;
        note.EncryptedContent = cipherText;
        note.IV = iv;

        var updated = await _noteRepository.UpdateAsync(note);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "UPDATE_NOTE",
            EntityType = "Note",
            EntityId = noteId,
            UserId = requestingUserId,
            Success = true,
        });

        return updated;
    }

    public async Task DeleteAsync(Guid noteId, Guid requestingUserId)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);

        if (note is null)
            throw new KeyNotFoundException($"Note {noteId} not found.");

        if (note.UserId != requestingUserId)
        {
            await LogFailedAccessAsync(noteId, requestingUserId, "DELETE_NOTE", "Access denied");
            throw new UnauthorizedAccessException("Access denied.");
        }

        await _noteRepository.DeleteAsync(noteId);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "DELETE_NOTE",
            EntityType = "Note",
            EntityId = noteId,
            UserId = requestingUserId,
            Success = true,
        });
    }

    public async Task<string> ShareAsync(Guid noteId, Guid requestingUserId)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);

        if (note is null)
            throw new KeyNotFoundException($"Note {noteId} not found.");

        if (note.UserId != requestingUserId)
        {
            await LogFailedAccessAsync(noteId, requestingUserId, "SHARE_NOTE", "Access denied");
            throw new UnauthorizedAccessException("Access denied.");
        }

        note.IsShared = true;
        note.ShareToken = Guid.NewGuid().ToString("N");
        await _noteRepository.UpdateAsync(note);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "SHARE_NOTE",
            EntityType = "Note",
            EntityId = noteId,
            UserId = requestingUserId,
            Success = true,
        });

        return note.ShareToken;
    }

    public async Task<Note> GetByShareTokenAsync(string token)
    {
        var note = await _noteRepository.GetByShareTokenAsync(token);

        if (note is null || !note.IsShared)
            throw new KeyNotFoundException("Shared note not found.");

        note.EncryptedContent = _encryptionService.Decrypt(note.EncryptedContent, note.IV);
        return note;
    }

    private async Task LogFailedAccessAsync(Guid entityId, Guid userId, string action, string details)
    {
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = action,
            EntityType = "Note",
            EntityId = entityId,
            UserId = userId,
            Success = false,
            Details = details,
        });
    }
}