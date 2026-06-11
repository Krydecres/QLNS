import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Holiday } from '../models/holiday.model';

@Injectable({
  providedIn: 'root'
})
export class HolidayService {
  private apiUrl = 'http://localhost:5294/api/Holidays';
  private http = inject(HttpClient);

  getHolidays(): Observable<Holiday[]> {
    return this.http.get<Holiday[]>(this.apiUrl);
  }

  getHoliday(id: number): Observable<Holiday> {
    return this.http.get<Holiday>(`${this.apiUrl}/${id}`);
  }

  createHoliday(holiday: Holiday): Observable<{message: string}> {
    return this.http.post<{message: string}>(this.apiUrl, holiday);
  }

  updateHoliday(id: number, holiday: Holiday): Observable<{message: string}> {
    return this.http.put<{message: string}>(`${this.apiUrl}/${id}`, holiday);
  }

  deleteHoliday(id: number): Observable<{message: string}> {
    return this.http.delete<{message: string}>(`${this.apiUrl}/${id}`);
  }
}
