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

export interface EmployeeDashboardProfile {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string;
  avatarUrl?: string;
  departmentName: string;
  positionName: string;
}

export interface EmployeeTodayShift {
  shiftName: string;
  startTime: string;
  endTime: string;
  note?: string;
}

export interface EmployeeSalarySummary {
  baseSalary: number;
  allowance: number;
  deduction: number;
  totalSalary: number;
}

export interface EmployeeAttendanceItem {
  date: string;
  checkInTime: string;
  checkOutTime: string;
  status: string;
  note?: string;
}

export interface EmployeeUpcomingLeave {
  startDate: string;
  endDate: string;
  reason: string;
}

export interface EmployeeDashboardData {
  employee: EmployeeDashboardProfile;
  currentMonth: number;
  currentYear: number;
  workDaysThisMonth: number;
  workingDayTarget: number;
  attendanceStatus: string;
  todayCheckInTime: string;
  todayCheckOutTime: string;
  todayShift: EmployeeTodayShift | null;
  salaryThisMonth: EmployeeSalarySummary | null;
  leaveSummary: LeaveSummary;
  recentAttendance: EmployeeAttendanceItem[];
  upcomingLeaves: EmployeeUpcomingLeave[];
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

  getEmployeeDashboard(username: string): Observable<EmployeeDashboardData> {
    return this.http.get<EmployeeDashboardData>(
      `${this.apiUrl}/employee/${encodeURIComponent(username)}`
    );
  }
}