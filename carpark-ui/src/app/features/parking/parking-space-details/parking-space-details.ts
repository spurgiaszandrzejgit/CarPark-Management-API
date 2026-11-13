
import {
  Component,
  inject,
  input,
  output,
  signal,
  OnInit
} from '@angular/core';

import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import {
  ParkingApiService,
  ParkingSpaceDetails as ParkingSpaceDetailsDto
} from '../../../core/services/parking-api';

@Component({
  selector: 'app-parking-space-details',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule],
  templateUrl: './parking-space-details.html',
  styleUrl: './parking-space-details.scss'
})
export class ParkingSpaceDetails implements OnInit {
  private readonly parkingApi = inject(ParkingApiService);
  private readonly fb = inject(FormBuilder);

  readonly spaceNumber = input.required<number>();
  readonly closed = output<void>();
  readonly parkingChanged = output<void>();

  readonly details = signal<ParkingSpaceDetailsDto | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly operationCompleted = output<string>();

  readonly parkForm = this.fb.nonNullable.group({
    vehicleReg: ['', [Validators.required, Validators.maxLength(20)]],
    vehicleType: [1, Validators.required]
  });

  ngOnInit(): void {
    this.loadDetails();
  }

  loadDetails(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getSpaceDetails(this.spaceNumber()).subscribe({
      next: details => {
        this.details.set(details);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parking space details.');
        this.loading.set(false);
      }
    });
  }


  parkVehicle(): void {
    if (this.parkForm.invalid || this.submitting()) {
      this.parkForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.success.set(null);

    const request = this.parkForm.getRawValue();

    this.parkingApi.parkVehicle(request).subscribe({
      next: response => {
        this.submitting.set(false);

        this.operationCompleted.emit(
          `Vehicle ${response.vehicleReg} parked in P${String(response.spaceNumber).padStart(2, '0')}.`
        );

        this.parkingChanged.emit();
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        console.error('Parking failed:', err);

        const apiError = err.error;

        const message =
          typeof apiError === 'string'
            ? apiError
            : apiError?.message ??
              apiError?.detail ??
              'Unable to park vehicle. Please try again.';

        this.error.set(message);
        this.submitting.set(false);
      }
    });
  }

  exitVehicle(): void {
    const vehicleReg = this.details()?.vehicleReg;

    if (!vehicleReg || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.success.set(null);

    this.parkingApi.exitVehicle({ vehicleReg }).subscribe({
      next: response => {
        this.submitting.set(false);

        this.operationCompleted.emit(
          `Vehicle ${response.vehicleReg} exited. Total charge: £${response.vehicleCharge.toFixed(2)}.`
        );

        this.parkingChanged.emit();
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        console.error('Exit vehicle failed:', err);

        const apiError = err.error;

        const message =
          typeof apiError === 'string'
            ? apiError
            : apiError?.message ??
              apiError?.detail ??
              'Unable to exit vehicle. Please try again.';

        this.error.set(message);
        this.submitting.set(false);
      }
    });
  }


  close(): void {
    this.closed.emit();
  }
}
