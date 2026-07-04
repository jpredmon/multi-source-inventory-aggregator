import { Component, signal } from '@angular/core';

import { InventoryTableComponent } from './inventory/inventory-table/inventory-table.component';

@Component({
  selector: 'app-root',
    // No `standalone: true` here — unlike InventoryTableComponent, which set
  // it explicitly. In the Angular version this project scaffolded with,
  // standalone is the default for every component; explicitly writing
  // `standalone: true` (as the table component's plan/spec did) is now
  // redundant, not required. Both forms compile to the same thing — this
  // file is just relying on the default instead of stating it.

  // This is the piece that actually wires the child component in: without
  // listing InventoryTableComponent here, <app-inventory-table> in app.html
  // below would be unrecognized markup — Angular would render it as a plain,
  // meaningless custom element with nothing inside. This `imports` array is
  // standalone components' replacement for the old NgModule `declarations`/
  // `imports` arrays — no AppModule class anywhere in this project at all.
  imports: [InventoryTableComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
   // Leftover from the Angular CLI's default scaffold (`ng new` generates
  // this in every new app) — a signal holding the app's name, presumably
  // meant to be displayed somewhere. Check app.html below: it's never
  // referenced. Harmless, unused boilerplate, not a bug — just CLI-generated
  // ceremony nobody deleted since it doesn't hurt anything.
  protected readonly title = signal('vin-web');
}
