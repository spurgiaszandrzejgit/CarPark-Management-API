
import { Component, inject, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import {
  ParkingApiService,
  ParkVehicleResponse
} from '../../../core/services/parking-api';
import { getApiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-park-vehicle',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './park-vehicle.html',
  styleUrl: './park-vehicle.scss'
})
export class ParkVehicle {
  private readonly parkingApi = inject(ParkingApiService);

  readonly parked = output<void>();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ParkVehicleResponse | null>(null);

  readonly form = new FormGroup({
    vehicleReg: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    vehicleType: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.parkingApi.parkVehicle(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.result.set(response);
        this.loading.set(false);
        this.form.reset();
        this.parked.emit();
      },
      error: err => {
        this.error.set(getApiErrorMessage(err));
        this.loading.set(false);
      }
    });
  }
}
