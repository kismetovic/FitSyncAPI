namespace FITSync.Domain.Models;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
