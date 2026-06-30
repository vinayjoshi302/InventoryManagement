using InventoryHold.Contracts;
using InventoryHold.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryHoldService _service;

    public InventoryController(InventoryHoldService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<InventoryListResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetInventoryAsync(cancellationToken);
        return Ok(result);
    }
}
