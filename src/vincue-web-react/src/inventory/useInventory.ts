import { useEffect, useState } from 'react';

import { getInventory } from './inventory.service';
import type { VehicleSummary } from './inventory.model';

export function useInventory(): VehicleSummary[] {
  const [vehicles, setVehicles] = useState<VehicleSummary[]>([]);

  useEffect(() => {
    getInventory().then(setVehicles);
  }, []);

  return vehicles;
}
