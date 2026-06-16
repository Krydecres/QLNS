import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  DashboardService,
  EmployeeAttendanceItem,
  EmployeeDashboardData
} from '../services/dashboard.service';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './employee-dashboard.component.html',
  styleUrls: ['./employee-dashboard.component.css']
})
export class EmployeeDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  dashboard: EmployeeDashboardData | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    const username = this.authService.username();

    if (!username) {
      this.errorMessage = 'Không tìm thấy thông tin đăng nhập.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.dashboardService.getEmployeeDashboard(username)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (data) => {
          this.dashboard = {
            ...data,
            recentAttendance: data.recentAttendance || [],
            upcomingLeaves: data.upcomingLeaves || [],
            leaveSummary: data.leaveSummary || {
              pending: 0,
              approved: 0,
              rejected: 0
            }
          };

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Lỗi khi tải dashboard nhân viên:', err);
          this.errorMessage = err.error?.message || 'Không thể tải dữ liệu dashboard nhân viên.';
          this.cdr.detectChanges();
        }
      });
  }

  getRecentAttendance(): EmployeeAttendanceItem[] {
    return this.dashboard?.recentAttendance || [];
  }

  getWorkDayPercent(): number {
    if (!this.dashboard || this.dashboard.workingDayTarget === 0) {
      return 0;
    }

    return Math.min(
      (this.dashboard.workDaysThisMonth / this.dashboard.workingDayTarget) * 100,
      100
    );
  }

  getSalaryTotal(): number {
    return this.dashboard?.salaryThisMonth?.totalSalary || 0;
  }

  getTodayShiftText(): string {
    if (!this.dashboard?.todayShift) {
      return 'Chưa phân ca';
    }

    return this.dashboard.todayShift.shiftName;
  }

  getTodayShiftTime(): string {
    if (!this.dashboard?.todayShift) {
      return 'Chưa có thông tin ca làm hôm nay';
    }

    return `${this.dashboard.todayShift.startTime} - ${this.dashboard.todayShift.endTime}`;
  }
}