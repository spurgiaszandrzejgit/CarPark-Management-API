import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environment/environment';

export interface ParkingStatus {
  availableSpaces: number;
  occupiedSpaces: number;
}

@Injectable({
  providedIn: 'root'
})
export class ParkingApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/parking`;

  getStatus(): Observable<ParkingStatus> {
    return this.http.get<ParkingStatus>(this.apiUrl);
  }

  parkVehicle(request: ParkVehicleRequest): Observable<ParkVehicleResponse> {
    return this.http.post<ParkVehicleResponse>(
      this.apiUrl,
      request
    );
  }

  exitVehicle(request: ExitVehicleRequest): Observable<ExitVehicleResponse> {
    return this.http.post<ExitVehicleResponse>(
      `${this.apiUrl}/exit`,
      request
    );
  }

  getSpaces(): Observable<ParkingSpace[]> {
    return this.http.get<ParkingSpace[]>(
      `${environment.apiUrl}/parking/spaces`
    );
  }

  getSpaceDetails(spaceNumber: number): Observable<ParkingSpaceDetails> {
    return this.http.get<ParkingSpaceDetails>(
      `${environment.apiUrl}/parking/spaces/${spaceNumber}`
    );
  }
}

export interface ParkVehicleRequest {
  vehicleReg: string;
  vehicleType: number;
}

export interface ParkVehicleResponse {
  vehicleReg: string;
  spaceNumber: number;
  timeIn: string;
}

export interface ExitVehicleRequest {
  vehicleReg: string;
}

export interface ExitVehicleResponse {
  vehicleReg: string;
  vehicleCharge: number;
  timeIn: string;
  timeOut: string;
}

export interface ParkingSpace {
  spaceNumber: number;
  isOccupied: boolean;
  vehicleReg: string | null;
}

export interface ParkingSpaceDetails {
  spaceNumber: number;
  isOccupied: boolean;
  vehicleReg: string | null;
  vehicleType: number | null;
  timeIn: string | null;
  currentCharge: number | null;
}