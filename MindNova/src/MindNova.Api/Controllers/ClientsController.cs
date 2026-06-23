using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Domain.Validation;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = MapFromCreateRequest(request);
        var validationErrors = ClientValidator.Validate(client);
        if (validationErrors.Count > 0)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validationErrors),
                Status = 400
            });
        }

        var created = await _clientService.CreateAsync(client);
        return Ok(MapToResponse(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Client not found",
                Detail = $"No client with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(client));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "search")] string search = null,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery(Name = "include_archived")] bool includeArchived = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (clients, totalCount) = await _clientService.ListAsync(search, page, pageSize, includeArchived);

        return Ok(new PagedResponse<ClientResponse>
        {
            Items = clients.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var updated = MapFromUpdateRequest(request);
        var validationErrors = ClientValidator.Validate(updated);
        if (validationErrors.Count > 0)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validationErrors),
                Status = 400
            });
        }

        var result = await _clientService.UpdateAsync(id, updated);
        if (result == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Client not found",
                Detail = $"No client with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var result = await _clientService.ArchiveAsync(id);
        if (result == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Client not found",
                Detail = $"No client with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(result));
    }

    private static Client MapFromCreateRequest(CreateClientRequest request)
    {
        return new Client
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Address = request.Address
        };
    }

    private static Client MapFromUpdateRequest(UpdateClientRequest request)
    {
        return new Client
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Address = request.Address
        };
    }

    private static ClientResponse MapToResponse(Client client)
    {
        return new ClientResponse
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            DateOfBirth = client.DateOfBirth,
            Phone = client.Phone,
            EmergencyContactName = client.EmergencyContactName,
            EmergencyContactPhone = client.EmergencyContactPhone,
            Address = client.Address,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt,
            IsArchived = client.IsArchived
        };
    }
}
