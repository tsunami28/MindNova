using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly MindNovaDbContext _context;

    public ClientService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<Client> CreateAsync(Client client)
    {
        client.Id = Guid.NewGuid();
        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;
        client.IsArchived = false;

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return client;
    }

    public async Task<Client> GetByIdAsync(Guid id)
    {
        return await _context.Clients.FindAsync(id);
    }

    public async Task<(List<Client> Items, int TotalCount)> ListAsync(string search, int page, int pageSize, bool includeArchived)
    {
        var query = _context.Clients.AsQueryable();

        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Email.Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Client> UpdateAsync(Guid id, Client updated)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return null;

        client.FirstName = updated.FirstName;
        client.LastName = updated.LastName;
        client.Email = updated.Email;
        client.DateOfBirth = updated.DateOfBirth;
        client.Phone = updated.Phone;
        client.EmergencyContactName = updated.EmergencyContactName;
        client.EmergencyContactPhone = updated.EmergencyContactPhone;
        client.Address = updated.Address;
        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return client;
    }

    public async Task<Client> ArchiveAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return null;

        client.IsArchived = true;
        client.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return client;
    }
}
