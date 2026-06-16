import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  EmployeeShiftService,
  MyShiftItem,
  MyShiftsResponse
} from '../services/employee-shift.service';

@Component({
  selector: 'app-my-shifts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-shifts.html',
  styleUrl: './my-shifts.css',
})
export class MyShifts implements OnInit {
  private authService = inject(AuthService);
  private employeeShiftService = inject(EmployeeShiftService);
  private cdr = inject(ChangeDetectorRef);

  data: MyShiftsResponse | null = null;
  startDate = this.getFirstDayOfCurrentMonth();
  endDate = this.getLastDayOfCurrentMonth();

  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadMyShifts();
  }

  loadMyShifts(): void {
    const username = this.authService.username();

    if (!username) {
      this.errorMessage = 'Không tìm thấy thông tin đăng nhập.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.employeeShiftService.getMyShifts(username, this.startDate, this.endDate)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.data = {
            ...res,
            shifts: res.shifts || []
          };
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Lỗi khi tải lịch ca làm:', err);
          this.errorMessage = err.error?.message || 'Không thể tải lịch ca làm.';
          this.cdr.detectChanges();
        }
      });
  }

  clearFilter(): void {
    this.startDate = '';
    this.endDate = '';
    this.loadMyShifts();
  }

  resetThisMonth(): void {
    this.startDate = this.getFirstDayOfCurrentMonth();
    this.endDate = this.getLastDayOfCurrentMonth();
    this.loadMyShifts();
  }

  get shifts(): MyShiftItem[] {
    return this.data?.shifts || [];
  }

  get inactiveShifts(): number {
    return this.shifts.filter(item => !item.isActive).length;
  }

  getFirstDayOfCurrentMonth(): string {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1)
      .toISOString()
      .slice(0, 10);
  }

  getLastDayOfCurrentMonth(): string {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth() + 1, 0)
      .toISOString()
      .slice(0, 10);
  }
}