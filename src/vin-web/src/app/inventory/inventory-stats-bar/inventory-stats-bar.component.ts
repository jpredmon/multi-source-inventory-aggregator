import { CommonModule } from '@angular/common';
import { Component, signal, type OnInit } from '@angular/core';

import { InventoryService } from '../inventory.service';
import type { InventoryStats, VehicleStatus } from '../inventory.model';

@Component({
  selector: 'app-inventory-stats-bar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './inventory-stats-bar.component.html',
  styleUrl: './inventory-stats-bar.component.css'
})
export class InventoryStatsBarComponent implements OnInit {
  readonly stats = signal<InventoryStats | null>(null);

  constructor(private readonly inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getStats().subscribe((stats) => {
      this.stats.set(stats);
    });
  }

  // countsByStatus only contains entries for statuses that actually occur
  // (it's built from a SQL GROUP BY, which never emits empty groups) — a
  // status with zero vehicles right now is simply absent from the array,
  // not present with count 0. This looks it up and defaults to 0 so the
  // template never has to special-case "missing" vs. "zero."
  countFor(status: VehicleStatus): number {
    return this.stats()?.countsByStatus.find((c) => c.status === status)?.count ?? 0;
  }
}
