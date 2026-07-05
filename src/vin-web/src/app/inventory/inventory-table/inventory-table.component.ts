import { CommonModule } from '@angular/common';
import { Component, signal, type OnInit } from '@angular/core';

import { InventoryService } from '../inventory.service';
import type { VehicleSummary } from '../inventory.model';

@Component({
  selector: 'app-inventory-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './inventory-table.component.html',
  styleUrl: './inventory-table.component.css'
})
export class InventoryTableComponent implements OnInit {
  readonly vehicles = signal<VehicleSummary[]>([]);

  constructor(private readonly inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getAll().subscribe((vehicles) => {
      this.vehicles.set(vehicles);
    });
  }
}
