using System;

namespace POSSystem.Services.Interfaces;

/// <summary>
/// Manages the current tenant (business + branch + staff) context for multi-tenancy.
/// The BusinessId is set during license activation, BranchId during branch selection,
/// StaffId during staff PIN login.
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
    /// The current staff member ID. Null if no staff logged in.
    /// </summary>
    Guid? CurrentStaffId { get; }
    
    /// <summary>
    /// The current staff member name for display.
    /// </summary>
    string? CurrentStaffName { get; }
    
    /// <summary>
    /// Whether a valid business context is established.
    /// </summary>
    bool IsContextValid { get; }
    
    /// <summary>
    /// Whether a branch has been selected and locked.
    /// </summary>
    bool IsBranchSelected { get; }
    
    /// <summary>
    /// Whether a staff member is currently logged in.
    /// </summary>
    bool IsStaffLoggedIn { get; }
    
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
    /// Sets the staff context. Called after successful PIN login.
    /// </summary>
    /// <param name="staffId">The staff member ID.</param>
    /// <param name="staffName">The staff member name for display.</param>
    void SetStaffContext(Guid staffId, string staffName);
    
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
    /// Clears only the staff context (for staff logout/lock).
    /// </summary>
    void ClearStaffContext();
    
    /// <summary>
    /// Loads persisted business and branch context from storage.
    /// </summary>
    /// <returns>True if context was restored successfully.</returns>
    bool LoadPersistedContext();
}
