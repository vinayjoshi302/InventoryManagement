using InventoryHold.Contracts;
using InventoryHold.Domain;
using InventoryHold.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HoldsController : ControllerBase
{
    private readonly InventoryHoldService _service;

    public HoldsController(InventoryHoldService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HoldResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await _service.GetActiveHoldsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateHoldResponse>> Create([FromBody] CreateHoldRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateHoldAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InventoryHoldDomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldResponse>> Get(string holdId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetHoldByIdAsync(holdId, cancellationToken);
            return Ok(result);
        }
        catch (InventoryHoldDomainException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{holdId}")]
    public async Task<ActionResult<HoldResponse>> Delete(string holdId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ReleaseHoldAsync(holdId, cancellationToken);
            return Ok(result);
        }
        catch (InventoryHoldDomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
