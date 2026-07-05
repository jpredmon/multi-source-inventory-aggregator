export type VehicleStatus = 'OnLot' | 'Auctioned' | 'Sold';

export interface VehicleSummary {
  vin: string;
  stockNumber: string;
  cost: number;
  dateAcquired: string;

  hammerPrice: number | null;
  auctionDate: string | null;
  condition: string | null;

  salePrice: number | null;
  daysOnLot: number | null;
  soldDate: string | null;
  profitMargin: number | null;

  status: VehicleStatus;
}
