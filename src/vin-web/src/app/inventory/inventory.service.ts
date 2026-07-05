import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { VehicleSummary } from './inventory.model';

const API_BASE_URL = 'http://localhost:5080/api/inventory';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<VehicleSummary[]> {
    return this.http.get<VehicleSummary[]>(API_BASE_URL);
  }
}
