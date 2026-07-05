using Vin.Api.Dtos;

namespace Vin.Api.Services;

public interface IInventoryAggregationService
{
    Task<List<VehicleSummaryDto>> GetAllAsync();
    Task<VehicleSummaryDto?> GetByVinAsync(string vin);
    Task<InventoryStatsDto> GetStatsAsync();
}
