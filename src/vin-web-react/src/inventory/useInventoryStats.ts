import { useEffect, useState } from 'react';

import { getInventoryStats } from './inventory.service';
import type { InventoryStats } from './inventory.model';

// Same shape as useInventory.ts — fetch once on mount, hold the result in
// state. Starts as null (not an empty-shaped object) so the component can
// tell "haven't loaded yet" apart from "loaded, all-zero stats."
export function useInventoryStats(): InventoryStats | null {
  const [stats, setStats] = useState<InventoryStats | null>(null);

  useEffect(() => {
    getInventoryStats().then(setStats);
  }, []);

  return stats;
}
