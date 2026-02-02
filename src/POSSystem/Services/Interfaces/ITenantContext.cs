using System;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Manages the current tenant (business) context for multi-tenancy.
/// The BusinessId is set during license activation and used for data isolation.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current business ID. Null if not yet activated.
    /// </summary>
    Guid? CurrentBusinessId { get; }
    
    /// <summary>
    /// Whether a valid tenant context is established.
    /// </summary>
    bool IsContextValid { get; }
    
    /// <summary>
    /// Sets the business context. Called during license activation.
    /// </summary>
    /// <param name="businessId">The business ID from license activation.</param>
    void SetBusinessContext(Guid businessId);
    
    /// <summary>
    /// Clears the business context (for logout/deactivation).
    /// </summary>
    void ClearContext();
    
    /// <summary>
    /// Loads persisted business context from storage.
    /// </summary>
    /// <returns>True if context was restored successfully.</returns>
    bool LoadPersistedContext();
}
