namespace SecureNotes.Core.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }
        public bool Success { get; set; } = true;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
    }
}