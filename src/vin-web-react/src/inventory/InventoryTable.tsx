import './InventoryTable.css';
import { useInventory } from './useInventory';

// React has no built-in pipe system like Angular's `| currency` / `| date`.
// Intl.NumberFormat / Intl.DateTimeFormat are the browser's native
// formatters and the direct manual equivalent — created once, outside the
// component, so the same formatter instance is reused on every render
// instead of being reconstructed per cell per render.
const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
const shortDate = new Intl.DateTimeFormat('en-US', { dateStyle: 'short' });

export function InventoryTable() {
  // Calling the hook here is what subscribes this component to re-render
  // whenever setVehicles() fires inside useInventory — same trigger
  // relationship as reading vehicles() in the Angular template, just
  // expressed as a function call at the top of the component instead of
  // inside the markup.
  const vehicles = useInventory();

  return (
    <table>
      <thead>
        <tr>
          <th>VIN</th>
          <th>Stock #</th>
          <th>Cost</th>
          <th>Date Acquired</th>
          <th>Hammer Price</th>
          <th>Auction Date</th>
          <th>Condition</th>
          <th>Sale Price</th>
          <th>Days On Lot</th>
          <th>Sold Date</th>
          <th>Profit Margin</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {/* .map() is JSX's answer to *ngFor — there's no structural
            directive in React; you just produce an array of elements with
            plain JavaScript. The `key` prop is mandatory here: it's how
            React tells which <tr> is which across re-renders (so it can
            reuse/reorder actual DOM nodes instead of tearing everything
            down and rebuilding it), the same identity problem Angular's
            *ngFor solves internally without you having to supply anything
            by default. vin is a safe key since it's unique per vehicle. */}
        {vehicles.map((vehicle) => (
          <tr key={vehicle.vin}>
            <td>{vehicle.vin}</td>
            <td>{vehicle.stockNumber}</td>
            <td>{currency.format(vehicle.cost)}</td>
            <td>{shortDate.format(new Date(vehicle.dateAcquired))}</td>
            {/* Same null-check-then-format ternary as the Angular template's
                `vehicle.hammerPrice != null ? (vehicle.hammerPrice | currency) : '—'`
                — just calling the formatter function directly instead of
                piping through one. */}
            <td>{vehicle.hammerPrice != null ? currency.format(vehicle.hammerPrice) : '—'}</td>
            <td>{vehicle.auctionDate != null ? shortDate.format(new Date(vehicle.auctionDate)) : '—'}</td>
            <td>{vehicle.condition ?? '—'}</td>
            <td>{vehicle.salePrice != null ? currency.format(vehicle.salePrice) : '—'}</td>
            <td>{vehicle.daysOnLot ?? '—'}</td>
            <td>{vehicle.soldDate != null ? shortDate.format(new Date(vehicle.soldDate)) : '—'}</td>
            <td>{vehicle.profitMargin != null ? currency.format(vehicle.profitMargin) : '—'}</td>
            <td>{vehicle.status}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
