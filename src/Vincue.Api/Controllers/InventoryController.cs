using Microsoft.AspNetCore.Mvc;
using Vin.Api.Dtos;
using Vin.Api.Services;

namespace Vin.Api.Controllers;
// [ApiController] turns on a bundle of API-specific conventions:
// automatic 400 responses for invalid model binding, inferring [FromBody]/
// [FromRoute] sources without you writing them explicitly, and requiring
// attribute routing (no convention-based routing). None of that is visible
// in this file — it's all implicit behavior this attribute switches on.
[ApiController]
// The route template for every action in this controller. Combined with
// [HttpGet]/[HttpGet("{vin}")] below, this produces the two real routes:
// GET /api/inventory and GET /api/inventory/{vin}.
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
     // Depends on the INTERFACE, not InventoryAggregationService directly.
    // The controller has no idea which concrete class it's talking to, or
    // that it happens to hit a database at all — it just knows "something
    // that can GetAllAsync() and GetByVinAsync(vin)". That's what makes this
    // controller trivially testable with a fake service and swappable without
    // touching this file.
    private readonly IInventoryAggregationService _service;
     // ASP.NET Core's DI container calls this constructor itself, per request —
    // you never write `new InventoryController(...)` anywhere. It resolves
    // the interface argument via the registration in Program.cs:
    //   builder.Services.AddScoped<IInventoryAggregationService, InventoryAggregationService>();

    public InventoryController(IInventoryAggregationService service)
    {
        _service = service;
    }

    // [HttpGet] with no template + the class-level [Route] = GET /api/inventory.
    // ActionResult<T> is a union type: this method can either "return Ok(dto)"
    // (200 + body, since dto is a T) or return a bare ActionResult like
    // NotFound() (no body needed) — the compiler and the framework both
    // understand both cases from one return type.

    [HttpGet]
    public async Task<ActionResult<List<VehicleSummaryDto>>> GetAll()
    {
        // Ok(...) wraps the list in a 200 response; ASP.NET Core's default
        // output formatter (System.Text.Json) then serializes it to JSON
        // for the response body. This line is the entire "translate C# to
        // wire format" step — no manual JSON construction anywhere.
        return Ok(await _service.GetAllAsync());
    }
    // "{vin}" is a route parameter — ASP.NET Core's model binder matches it
    // to the `vin` method parameter by name (not position, not type alone).
    // GET /api/inventory/1G1ZD5ST0LF123456 binds vin = "1G1ZD5ST0LF123456".
    [HttpGet("{vin}")]
    public async Task<ActionResult<VehicleSummaryDto>> GetByVin(string vin)
    {
        var result = await _service.GetByVinAsync(vin);
        // The controller's only real decision: translate "service found
        // nothing" (null) into an HTTP-meaningful 404, rather than leaking
        // a null-shaped 200 body to the client.
        return result is null ? NotFound() : Ok(result);
    }

    // A literal route segment ("stats") ranks above a parameter segment
    // ({vin}) in ASP.NET Core's attribute-routing precedence, so
    // GET /api/inventory/stats resolves here, not into GetByVin with
    // vin = "stats" — worth confirming with a real request rather than
    // assuming, since the two routes look ambiguous at a glance.
    [HttpGet("stats")]
    public async Task<ActionResult<InventoryStatsDto>> GetStats()
    {
        return Ok(await _service.GetStatsAsync());
    }
}
