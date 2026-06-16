import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AttendanceChartItem {
  date: string;
  count: number;
}

export interface DepartmentChartItem {
  departmentName: string;
  count: number;
}

export interface LeaveSummary {
  pending: number;
  approved: number;
  rejected: number;
}

export interface AdminDashboardData {
  totalEmployees: number;
  totalDepartments: number;
  totalPositions: number;
  presentToday: number;
  approvedLeaveToday: number;
  notCheckedInToday: number;
  attendanceRate: number;
  pendingLeaveRequests: number;
  totalSalaryThisMonth: number;
  currentMonth: number;
  currentYear: number;
  recentAttendance: AttendanceChartItem[];
  employeesByDepartment: DepartmentChartItem[];
  leaveSummary: LeaveSummary;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5294/api/Dashboard';

  getAdminDashboard(): Observable<AdminDashboardData> {
    return this.http.get<AdminDashboardData>(`${this.apiUrl}/admin`);
  }
}