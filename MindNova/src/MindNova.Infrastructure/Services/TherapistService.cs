using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class TherapistService : ITherapistService
{
    private readonly MindNovaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TherapistService(MindNovaDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<(TherapistProfile Profile, string Error)> CreateAsync(string userId, List<string> specialisations, int maxCaseload)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (null, $"No user with ID {userId} exists.");
        }

        var existing = await _context.TherapistProfiles.FirstOrDefaultAsync(t => t.UserId == userId);
        if (existing != null)
        {
            return (null, $"A therapist profile already exists for user {userId}.");
        }

        var profile = new TherapistProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Specialisations = specialisations,
            MaxCaseload = maxCaseload,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TherapistProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return (profile, null);
    }

    public async Task<TherapistProfile> GetByIdAsync(Guid id)
    {
        return await _context.TherapistProfiles.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(List<TherapistProfile> Items, int TotalCount)> ListAsync(int page, int pageSize, bool includeInactive)
    {
        var query = _context.TherapistProfiles.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(TherapistProfile Profile, string Error)> UpdateAsync(Guid id, List<string> specialisations, int maxCaseload)
    {
        var profile = await _context.TherapistProfiles.FirstOrDefaultAsync(t => t.Id == id);
        if (profile == null)
        {
            return (null, $"No therapist profile with ID {id} exists.");
        }

        profile.Specialisations = specialisations;
        profile.MaxCaseload = maxCaseload;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (profile, null);
    }

    public async Task<TherapistProfile> DeactivateAsync(Guid id)
    {
        var profile = await _context.TherapistProfiles.FirstOrDefaultAsync(t => t.Id == id);
        if (profile == null)
        {
            return null;
        }

        profile.IsActive = false;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return profile;
    }

    public async Task<List<CaseloadSummary>> GetCaseloadAsync()
    {
        var activeProfiles = await _context.TherapistProfiles
            .Where(t => t.IsActive)
            .ToListAsync();

        var summaries = new List<CaseloadSummary>();

        foreach (var profile in activeProfiles)
        {
            var currentCaseload = await _context.Clients.CountAsync(c => c.AssignedTherapistId == profile.Id);
            var user = await _userManager.FindByIdAsync(profile.UserId);

            summaries.Add(new CaseloadSummary
            {
                TherapistProfileId = profile.Id,
                TherapistName = user?.Email ?? string.Empty,
                MaxCaseload = profile.MaxCaseload,
                CurrentCaseload = currentCaseload,
                AvailableCapacity = profile.MaxCaseload - currentCaseload
            });
        }

        return summaries;
    }
}
