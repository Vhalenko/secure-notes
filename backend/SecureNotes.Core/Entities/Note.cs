using SecureNotes.Core.Enums;

namespace SecureNotes.Core.Entities
{
    public class Note
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string EncryptedContent { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
        public NoteStatus Status { get; set; } = NoteStatus.Active;
        public bool IsShared { get; set; } = false;
        public string? ShareToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}