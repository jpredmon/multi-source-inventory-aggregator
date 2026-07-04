import { Component, signal } from '@angular/core';

import { InventoryTableComponent } from './inventory/inventory-table/inventory-table.component';

@Component({
  selector: 'app-root',
  imports: [InventoryTableComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('vin-web');
}
