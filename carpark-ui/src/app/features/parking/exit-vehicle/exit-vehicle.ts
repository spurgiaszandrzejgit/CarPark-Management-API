
import { DatePipe, CurrencyPipe } from '@angular/common';
import { Component, inject, output, signal } from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  ParkingApiService,
  ExitVehicleResponse
} from '../../../core/services/parking-api';

import { getApiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-exit-vehicle',
  imports: [ReactiveFormsModule, DatePipe, CurrencyPipe],
  templateUrl: './exit-vehicle.html',
  styleUrl: './exit-vehicle.scss'
})
export class ExitVehicle {
  private readonly parkingApi = inject(ParkingApiService);

  readonly exited = output<void>();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ExitVehicleResponse | null>(null);

  readonly form = new FormGroup({
    vehicleReg: new FormControl('', {
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

    this.parkingApi.exitVehicle(this.form.getRawValue()).subscribe({
      next: response => {
        this.result.set(response);
        this.loading.set(false);
        this.form.reset();
        this.exited.emit();
      },
      error: err => {
        this.error.set(getApiErrorMessage(err));
        this.loading.set(false);
      }
    });
  }
}
