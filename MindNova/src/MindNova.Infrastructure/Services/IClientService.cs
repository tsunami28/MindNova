using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface IClientService
{
    Task<Client> CreateAsync(Client client);
    Task<Client> GetByIdAsync(Guid id);
    Task<(List<Client> Items, int TotalCount)> ListAsync(string search, int page, int pageSize, bool includeArchived);
    Task<Client> UpdateAsync(Guid id, Client updated);
    Task<Client> ArchiveAsync(Guid id);
}
