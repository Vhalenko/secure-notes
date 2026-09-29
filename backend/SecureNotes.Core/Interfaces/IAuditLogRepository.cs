using SecureNotes.Core.Entities;

namespace SecureNotes.Core.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog log);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<AuditLog>> GetAllAsync();
    }
}