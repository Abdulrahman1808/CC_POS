using System;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Manages the current tenant (business + branch) context for multi-tenancy.
/// The BusinessId is set during license activation, BranchId during branch selection.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current business ID. Null if not yet activated.
    /// </summary>
    Guid? CurrentBusinessId { get; }
    
    /// <summary>
    /// The current branch ID. Null if not yet selected.
    /// </summary>
    Guid? CurrentBranchId { get; }
    
    /// <summary>
    /// Whether a valid business context is established.
    /// </summary>
    bool IsContextValid { get; }
    
    /// <summary>
    /// Whether a branch has been selected and locked.
    /// </summary>
    bool IsBranchSelected { get; }
    
    /// <summary>
    /// Whether the full context (business + branch) is ready for operations.
    /// </summary>
    bool IsFullyConfigured { get; }
    
    /// <summary>
    /// Sets the business context. Called during license activation.
    /// </summary>
    /// <param name="businessId">The business ID from license activation.</param>
    void SetBusinessContext(Guid businessId);
    
    /// <summary>
    /// Sets the branch context. Called after branch selection UI.
    /// </summary>
    /// <param name="branchId">The selected branch ID.</param>
    /// <param name="branchName">The branch name for display.</param>
    void SetBranchContext(Guid branchId, string branchName);
    
    /// <summary>
    /// Gets the cached branch name for display purposes.
    /// </summary>
    string? CurrentBranchName { get; }
    
    /// <summary>
    /// Clears the business and branch context (for logout/deactivation).
    /// </summary>
    void ClearContext();
    
    /// <summary>
    /// Clears only the branch context (for branch re-selection).
    /// </summary>
    void ClearBranchContext();
    
    /// <summary>
    /// Loads persisted business and branch context from storage.
    /// </summary>
    /// <returns>True if context was restored successfully.</returns>
    bool LoadPersistedContext();
}
