using Microsoft.AspNetCore.Mvc;
using Vin.Api.Dtos;
using Vin.Api.Services;

namespace Vin.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryAggregationService _service;

    public InventoryController(IInventoryAggregationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleSummaryDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{vin}")]
    public async Task<ActionResult<VehicleSummaryDto>> GetByVin(string vin)
    {
        var result = await _service.GetByVinAsync(vin);
        return result is null ? NotFound() : Ok(result);
    }
}
