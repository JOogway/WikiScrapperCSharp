namespace WikiScrapper.Domain.Entities;

/// <summary>
/// Base class for all persisted entities, providing identity and audit timestamps.
/// </summary>
public abstract class Entity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>UTC timestamp of when the row was created. Set once by the persistence layer.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last modification. Maintained automatically by the persistence layer.</summary>
    public DateTime UpdatedAt { get; set; }
}
