import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TimekeepingDto {
  id?: number;
  username: string;
  fullName: string;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status?: string;
  note?: string;
}

export interface UserDto {
  username: string;
  fullName: string;
}

@Injectable({
  providedIn: 'root'
})
export class TimekeepingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/Timekeepings'; // Updated to match actual API port

  getMyAttendance(username: string, startDate?: string, endDate?: string): Observable<TimekeepingDto[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<TimekeepingDto[]>(`${this.apiUrl}/${username}`, { params });
  }

  getAllAttendance(startDate?: string, endDate?: string): Observable<TimekeepingDto[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<TimekeepingDto[]>(`${this.apiUrl}`, { params });
  }

  submitManualEntry(data: TimekeepingDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/manual-entry`, data);
  }

  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.apiUrl}/users`);
  }

  checkIn(username: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/check-in/${username}`, {});
  }

  checkOut(username: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/check-out/${username}`, {});
  }

  exportExcel(startDate?: string, endDate?: string): Observable<Blob> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get(`${this.apiUrl}/export`, { params, responseType: 'blob' });
  }
}
