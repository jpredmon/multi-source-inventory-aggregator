import type { VehicleSummary } from './inventory.model';

const API_BASE_URL = 'http://localhost:5080/api/inventory';

// A plain exported function, not an @Injectable class like Angular's
// InventoryService. React has no built-in DI container — there's nothing to
// register this with, and nothing constructs it for you. Whatever calls
// getInventory() just imports the function directly, the same way you'd
// import any other utility.
export async function getInventory(): Promise<VehicleSummary[]> {
  // fetch() is the browser's native HTTP client — the direct equivalent of
  // Angular's HttpClient.get(), but unlike HttpClient, fetch() does NOT
  // reject its promise on a non-2xx response (a 404 or 500 resolves
  // successfully, with response.ok === false). That check has to be done
  // manually here; HttpClient does the equivalent for you internally and
  // surfaces errors through the Observable's error channel instead.
  const response = await fetch(API_BASE_URL);
  if (!response.ok) {
    throw new Error(`Failed to load inventory: ${response.status}`);
  }
  // .json() parses the response body — this is where the plain JS objects
  // get produced; the <VehicleSummary[]> in this function's return type is
  // still just a compile-time label on top of them, not real conversion.
  return response.json();
}
