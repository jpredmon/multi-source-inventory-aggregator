import { useEffect, useState } from 'react';

import { getInventory } from './inventory.service';
import type { VehicleSummary } from './inventory.model';

// This hook is the idiomatic-React analogue of Angular's
// `signal<VehicleSummary[]>([])` + `ngOnInit()` + `.subscribe()` combo —
// same job (fetch once, hold the result, trigger a re-render when it
// arrives), different mechanism.
export function useInventory(): VehicleSummary[] {
  // useState is React's equivalent of a signal: it returns the current value
  // plus a setter, and calling the setter is what tells React "something
  // changed, re-render whatever reads this." Reading a signal means calling
  // it (vehicles()); reading useState's value just means using the plain
  // variable (vehicles) — no function call needed, since useState re-runs
  // this whole hook function on every render rather than tracking a mutable
  // box the way a signal does.
  const [vehicles, setVehicles] = useState<VehicleSummary[]>([]);

  // useEffect with an empty dependency array ([]) means "run this once,
  // after the first render" — the direct equivalent of Angular's
  // ngOnInit(). There's no separate lifecycle method to override in React;
  // effects declared inside the component function ARE the lifecycle hooks.
  useEffect(() => {
    // getInventory() returns a Promise, not an RxJS Observable — .then()
    // here is doing the same job as Angular's .subscribe(), but Promises
    // are one-shot (resolve once) where Observables can emit repeatedly.
    // For a single GET request that difference doesn't matter, but it's why
    // React's ecosystem reaches for a library (React Query, etc.) instead of
    // raw Promises once you need retries, caching, or repeated emissions —
    // deliberately not used here, to keep this a fair comparison to
    // Angular's equally manual HttpClient.subscribe() pattern.
    getInventory().then(setVehicles);
  }, []);

  return vehicles;
}
