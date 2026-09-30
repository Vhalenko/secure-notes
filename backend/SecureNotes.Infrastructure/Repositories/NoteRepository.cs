using Microsoft.EntityFrameworkCore;
using SecureNotes.Core.Entities;
using SecureNotes.Core.Interfaces;
using SecureNotes.Infrastructure.Data;

namespace SecureNotes.Infrastructure.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly AppDbContext _context;

    public NoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Note?> GetByIdAsync(Guid id)
    {
        return await _context.Notes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Note>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetByShareTokenAsync(string token)
    {
        return await _context.Notes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.ShareToken == token);
    }

    public async Task<Note> CreateAsync(Note note)
    {
        note.Id = Guid.NewGuid();
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<Note> UpdateAsync(Note note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task DeleteAsync(Guid id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note is not null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }
}