import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EmployeeShiftItem {
  id: number;
  employeeId: number;
  employeeName: string;
  email: string;
  departmentName: string;
  positionName: string;
  shiftId: number;
  shiftName: string;
  startTime: string;
  endTime: string;
  wageMultiplier: number;
  workDate: string;
  note?: string;
  isActive: boolean;
}

export interface EmployeeShiftRequest {
  employeeId: number;
  shiftId: number;
  workDate: string;
  note?: string;
  isActive: boolean;
}

export interface EmployeeOption {
  id: number;
  fullName: string;
  email: string;
  departmentName: string;
  positionName: string;
}

export interface ShiftOption {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  wageMultiplier: number;
}

export interface EmployeeShiftOptions {
  employees: EmployeeOption[];
  shifts: ShiftOption[];
}

export interface MyShiftEmployee {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string;
  avatarUrl?: string;
  departmentName: string;
  positionName: string;
}

export interface MyShiftItem {
  id: number;
  shiftId: number;
  shiftName: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  wageMultiplier: number;
  workDate: string;
  dayOfWeek: string;
  note?: string;
  isActive: boolean;
}

export interface MyShiftsResponse {
  employee: MyShiftEmployee;
  startDate?: string;
  endDate?: string;
  totalShifts: number;
  activeShifts: number;
  shifts: MyShiftItem[];
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeShiftService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/EmployeeShifts';

  getEmployeeShifts(
    startDate?: string,
    endDate?: string,
    employeeId?: number,
    shiftId?: number
  ): Observable<EmployeeShiftItem[]> {
    let params = new HttpParams();

    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (employeeId) params = params.set('employeeId', employeeId);
    if (shiftId) params = params.set('shiftId', shiftId);

    return this.http.get<EmployeeShiftItem[]>(this.apiUrl, { params });
  }

  getOptions(): Observable<EmployeeShiftOptions> {
    return this.http.get<EmployeeShiftOptions>(`${this.apiUrl}/options`);
  }

  createEmployeeShift(model: EmployeeShiftRequest): Observable<{ message: string; id: number }> {
    return this.http.post<{ message: string; id: number }>(this.apiUrl, model);
  }

  updateEmployeeShift(id: number, model: EmployeeShiftRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, model);
  }

  deleteEmployeeShift(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
  getMyShifts(
  username: string,
  startDate?: string,
  endDate?: string
): Observable<MyShiftsResponse> {
  let params = new HttpParams();

  if (startDate) params = params.set('startDate', startDate);
  if (endDate) params = params.set('endDate', endDate);

  return this.http.get<MyShiftsResponse>(
    `${this.apiUrl}/my-shifts/${encodeURIComponent(username)}`,
    { params }
  );
}
}