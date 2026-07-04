import './InventoryTable.css';
import { useInventory } from './useInventory';

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
const shortDate = new Intl.DateTimeFormat('en-US', { dateStyle: 'short' });

export function InventoryTable() {
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
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {vehicles.map((vehicle) => (
          <tr key={vehicle.vin}>
            <td>{vehicle.vin}</td>
            <td>{vehicle.stockNumber}</td>
            <td>{currency.format(vehicle.cost)}</td>
            <td>{shortDate.format(new Date(vehicle.dateAcquired))}</td>
            <td>{vehicle.hammerPrice != null ? currency.format(vehicle.hammerPrice) : '—'}</td>
            <td>{vehicle.auctionDate != null ? shortDate.format(new Date(vehicle.auctionDate)) : '—'}</td>
            <td>{vehicle.condition ?? '—'}</td>
            <td>{vehicle.salePrice != null ? currency.format(vehicle.salePrice) : '—'}</td>
            <td>{vehicle.daysOnLot ?? '—'}</td>
            <td>{vehicle.soldDate != null ? shortDate.format(new Date(vehicle.soldDate)) : '—'}</td>
            <td>{vehicle.status}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
