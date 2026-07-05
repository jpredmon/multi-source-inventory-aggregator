import './InventoryStatsBar.css';
import { useInventoryStats } from './useInventoryStats';
import type { InventoryStats, VehicleStatus } from './inventory.model';

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
const days = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 });

// countsByStatus only contains entries for statuses that actually occur
// (built from a SQL GROUP BY, which never emits empty groups) — a status
// with zero vehicles right now is absent from the array, not present with
// count 0. This looks it up and defaults to 0, same helper Angular's
// InventoryStatsBarComponent has as a class method.
function countFor(stats: InventoryStats, status: VehicleStatus): number {
  return stats.countsByStatus.find((c) => c.status === status)?.count ?? 0;
}

export function InventoryStatsBar() {
  const stats = useInventoryStats();

  // Nothing renders until the one GET resolves — same "empty until loaded"
  // behavior as InventoryTable.
  if (stats == null) {
    return null;
  }

  return (
    <div className="stats-bar">
      <div>
        {countFor(stats, 'OnLot')} on lot
        {' • '}
        {countFor(stats, 'Auctioned')} auctioned
        {' • '}
        {countFor(stats, 'Sold')} sold
        {' • '}
        {currency.format(stats.totalCost)} total cost
      </div>
      <div>
        avg cost {currency.format(stats.averageCost)}
        {' • '}
        avg margin (sold) {stats.averageProfitMarginForSold != null ? currency.format(stats.averageProfitMarginForSold) : '—'}
        {' • '}
        avg {stats.averageDaysOnLotForSold != null ? days.format(stats.averageDaysOnLotForSold) : '—'} days on lot
      </div>
    </div>
  );
}
