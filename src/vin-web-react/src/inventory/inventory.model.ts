// Same shape as Angular's inventory.model.ts, field-for-field, because both
// frontends are consuming the identical VehicleSummaryDto from the API.
// This is a compile-time-only construct in TypeScript — it's erased entirely
// at build time, so there's no runtime "VehicleSummary" object anywhere; it's
// purely a label the type checker uses to validate property access on the
// plain JSON objects fetch() returns.
export type VehicleStatus = 'OnLot' | 'Auctioned' | 'Sold';

export interface VehicleSummary {
  // Always present — DealerInventory is the anchor on the API side.
  vin: string;
  stockNumber: string;
  cost: number;
  dateAcquired: string;

  // Nullable because auction/sale data is optional per vehicle. Same
  // left-join-shaped nullability as the API's VehicleSummaryDto and
  // Angular's identical interface.
  hammerPrice: number | null;
  auctionDate: string | null;
  condition: string | null;

  salePrice: number | null;
  daysOnLot: number | null;
  soldDate: string | null;
  profitMargin: number | null;

  status: VehicleStatus;
}

export interface StatusCount {
  status: VehicleStatus;
  count: number;
}

export interface InventoryStats {
  countsByStatus: StatusCount[];
  totalCost: number;
  averageCost: number;
  averageProfitMarginForSold: number | null;
  averageDaysOnLotForSold: number | null;
}
