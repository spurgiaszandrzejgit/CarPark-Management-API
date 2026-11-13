import { Component, OnInit, inject, output, signal } from '@angular/core';
import {
  ParkingApiService,
  ParkingSpace
} from '../../../core/services/parking-api';

@Component({
  selector: 'app-parking-lot',
  standalone: true,
  imports: [],
  templateUrl: './parking-lot.html',
  styleUrl: './parking-lot.scss'
})
export class ParkingLot implements OnInit {
  private readonly parkingApi = inject(ParkingApiService);

  readonly spaces = signal<ParkingSpace[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly spaceSelected = output<number>();

  ngOnInit(): void {
    this.loadSpaces();
  }

  loadSpaces(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getSpaces().subscribe({
      next: spaces => {
        this.spaces.set(spaces);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parking spaces.');
        this.loading.set(false);
      }
    });
  }

  selectSpace(spaceNumber: number): void {
    this.spaceSelected.emit(spaceNumber);
  }
}