import type { VehicleSummary } from './inventory.model';

const API_BASE_URL = 'http://localhost:5080/api/inventory';

export async function getInventory(): Promise<VehicleSummary[]> {
  const response = await fetch(API_BASE_URL);
  if (!response.ok) {
    throw new Error(`Failed to load inventory: ${response.status}`);
  }
  return response.json();
}
