using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using POSSystem.Data.Interfaces;
using POSSystem.Services.Interfaces;

namespace POSSystem.Services;

/// <summary>
/// Implements staff authentication via PIN.
/// </summary>
public class StaffService : IStaffService
{
    private readonly IDataService _dataService;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;

    public StaffService(
        IDataService dataService, 
        ITenantContext tenantContext,
        IAuditService auditService)
    {
        _dataService = dataService;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    /// <inheritdoc />
    public Guid? CurrentStaffId => _tenantContext.CurrentStaffId;

    /// <inheritdoc />
    public string? CurrentStaffName => _tenantContext.CurrentStaffName;

    /// <inheritdoc />
    public bool IsLoggedIn => _tenantContext.IsStaffLoggedIn;

    /// <inheritdoc />
    public async Task<bool> AuthenticateByPinAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        try
        {
            // Get all active staff members for the current branch
            var staffMembers = await _dataService.GetStaffMembersAsync();
            
            // Find staff with matching PIN (active only)
            var staff = staffMembers.FirstOrDefault(s => 
                s.IsActive && 
                s.Pin == pin &&
                (s.BranchId == null || s.BranchId == _tenantContext.CurrentBranchId));

            if (staff != null)
            {
                // Set staff context
                _tenantContext.SetStaffContext(staff.Id, staff.Name);
                
                // Update last login time
                staff.LastLoginAt = DateTime.UtcNow;
                await _dataService.UpdateStaffMemberAsync(staff);
                
                // Log the login
                await _auditService.LogStaffLoginAsync(staff.Id, staff.Name);

                Debug.WriteLine($"[StaffService] Authenticated: {staff.Name} ({staff.Level})");
                return true;
            }

            Debug.WriteLine("[StaffService] Authentication failed: Invalid PIN");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StaffService] Auth error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        if (_tenantContext.IsStaffLoggedIn)
        {
            var staffId = _tenantContext.CurrentStaffId!.Value;
            var staffName = _tenantContext.CurrentStaffName ?? "Unknown";

            // Log the logout
            await _auditService.LogStaffLogoutAsync(staffId, staffName);

            // Clear staff context
            _tenantContext.ClearStaffContext();

            Debug.WriteLine("[StaffService] Staff logged out");
        }
    }
}
