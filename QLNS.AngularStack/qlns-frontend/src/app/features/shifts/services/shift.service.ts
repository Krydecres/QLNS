import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ShiftItem {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  description?: string;
  wageMultiplier: number;
  isActive: boolean;
}

export interface ShiftRequest {
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  description?: string;
  wageMultiplier: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ShiftService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/Shifts';

  getShifts(search?: string): Observable<ShiftItem[]> {
    let params = new HttpParams();

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<ShiftItem[]>(this.apiUrl, { params });
  }

  createShift(model: ShiftRequest): Observable<{ message: string; id: number }> {
    return this.http.post<{ message: string; id: number }>(this.apiUrl, model);
  }

  updateShift(id: number, model: ShiftRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, model);
  }

  deleteShift(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  permanentDeleteShift(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}/permanent`);
  }
}