import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import {
  AdminDashboardData,
  AttendanceChartItem,
  DepartmentChartItem,
  DashboardService
} from '../services/dashboard.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private cdr = inject(ChangeDetectorRef);

  dashboard: AdminDashboardData | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    console.log('Đang gọi API Dashboard...');

    this.dashboardService.getAdminDashboard()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          console.log('Đã kết thúc gọi API, isLoading:', this.isLoading);

          // Ép Angular cập nhật lại giao diện sau khi tắt loading
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (data) => {
          console.log('Dữ liệu dashboard nhận được:', data);

          this.dashboard = {
            ...data,
            recentAttendance: data.recentAttendance || [],
            employeesByDepartment: data.employeesByDepartment || [],
            leaveSummary: data.leaveSummary || {
              pending: 0,
              approved: 0,
              rejected: 0
            }
          };

          console.log('Dashboard sau khi gán:', this.dashboard);

          // Ép cập nhật sau khi gán dữ liệu
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Lỗi khi tải dashboard:', err);
          this.errorMessage = 'Không thể tải dữ liệu dashboard.';
          this.cdr.detectChanges();
        }
      });
  }

  getRecentAttendance(): AttendanceChartItem[] {
    return this.dashboard?.recentAttendance || [];
  }

  getEmployeesByDepartment(): DepartmentChartItem[] {
    return this.dashboard?.employeesByDepartment || [];
  }

  getMaxAttendance(): number {
    const data = this.getRecentAttendance();

    if (data.length === 0) {
      return 0;
    }

    return Math.max(...data.map(item => item.count));
  }

  getAttendanceHeight(item: AttendanceChartItem): number {
    const max = this.getMaxAttendance();

    if (max === 0) {
      return 0;
    }

    return Math.max((item.count / max) * 100, 8);
  }

  getMaxDepartmentCount(): number {
    const data = this.getEmployeesByDepartment();

    if (data.length === 0) {
      return 0;
    }

    return Math.max(...data.map(item => item.count));
  }

  getDepartmentPercent(item: DepartmentChartItem): number {
    const max = this.getMaxDepartmentCount();

    if (max === 0) {
      return 0;
    }

    return (item.count / max) * 100;
  }
}