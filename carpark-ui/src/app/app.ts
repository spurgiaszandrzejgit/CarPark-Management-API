
import {
  Component,
  inject,
  OnInit,
  OnDestroy,
  signal,
  viewChild
} from '@angular/core';

import {
  ParkingApiService,
  ParkingStatus
} from './core/services/parking-api';

import { ParkingLot } from './features/parking/parking-lot/parking-lot';
import { ParkingSpaceDetails } from './features/parking/parking-space-details/parking-space-details';

@Component({
  selector: 'app-root',
  imports: [
    ParkingLot,
    ParkingSpaceDetails
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  private readonly parkingApi = inject(ParkingApiService);

  readonly parkingStatus = signal<ParkingStatus | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly parkingLot = viewChild(ParkingLot);
  readonly selectedSpaceNumber = signal<number | null>(null);
  readonly notification = signal<string | null>(null);

  private notificationTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadParkingStatus();
  }

  loadParkingStatus(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getStatus().subscribe({
      next: status => {
        this.parkingStatus.set(status);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to connect to the parking API.');
        this.loading.set(false);
      }
    });
  }

  refreshParking(): void {
    this.loadParkingStatus();
    this.parkingLot()?.loadSpaces();
  }

  selectSpace(spaceNumber: number): void {
    this.selectedSpaceNumber.set(spaceNumber);
  }

  closeSpaceDetails(): void {
    this.selectedSpaceNumber.set(null);
  }

  showNotification(message: string): void {
    this.dismissNotification();

    this.notification.set(message);

    this.notificationTimeout = setTimeout(() => {
      this.notification.set(null);
      this.notificationTimeout = null;
    }, 5000);
  }

  dismissNotification(): void {
    if (this.notificationTimeout !== null) {
      clearTimeout(this.notificationTimeout);
      this.notificationTimeout = null;
    }

    this.notification.set(null);
  }

  ngOnDestroy(): void {
    this.dismissNotification();
  }
}
