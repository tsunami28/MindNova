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

    public async Task<List<Client>> ListAsync()
    {
        return await _context.Clients
            .Where(c => !c.IsArchived)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
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
