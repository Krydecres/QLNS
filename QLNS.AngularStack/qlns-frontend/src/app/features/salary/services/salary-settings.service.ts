import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SalaryPosition {
  id: number;
  name: string;
  dailyWage: number;
}

export interface SalaryShift {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  wageMultiplier: number;
  isActive: boolean;
}

export interface SalarySettingsData {
  positions: SalaryPosition[];
  shifts: SalaryShift[];
}

@Injectable({
  providedIn: 'root'
})
export class SalarySettingsService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/SalarySettings';

  getSettings(): Observable<SalarySettingsData> {
    return this.http.get<SalarySettingsData>(this.apiUrl);
  }

  updatePositionDailyWage(id: number, dailyWage: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/positions/${id}/daily-wage`, { dailyWage });
  }

  updateShiftWageMultiplier(id: number, wageMultiplier: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/shifts/${id}/wage-multiplier`, { wageMultiplier });
  }
}
