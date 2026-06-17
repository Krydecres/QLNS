import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  EmployeeShiftService,
  MyShiftItem,
  MyShiftsResponse,
  ShiftOption
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

  shiftsOptions: ShiftOption[] = [];
  isRegistering = false;
  registerDate = this.getTodayString();
  selectedShiftId = 0;

  ngOnInit(): void {
    this.loadMyShifts();
    this.loadOptions();
  }

  loadOptions(): void {
    this.employeeShiftService.getOptions().subscribe({
      next: (data) => {
        this.shiftsOptions = data.shifts;
        this.cdr.detectChanges();
      }
    });
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

  toggleRegisterForm(): void {
    this.isRegistering = !this.isRegistering;
    this.errorMessage = '';
  }

  registerShift(): void {
    if (!this.data?.employee?.id) {
       this.errorMessage = 'Không tìm thấy thông tin nhân viên.';
       return;
    }
    if (!this.selectedShiftId) {
       this.errorMessage = 'Vui lòng chọn ca làm.';
       return;
    }
    if (!this.registerDate) {
       this.errorMessage = 'Vui lòng chọn ngày làm việc.';
       return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    const request = {
       employeeId: this.data.employee.id,
       shiftId: this.selectedShiftId,
       workDate: this.registerDate,
       isActive: true
    };

    this.employeeShiftService.createEmployeeShift(request).subscribe({
       next: (res) => {
          this.isRegistering = false;
          alert(res.message || 'Đăng ký ca làm thành công.');
          this.loadMyShifts();
       },
       error: (err) => {
          this.errorMessage = err.error?.message || 'Đăng ký ca làm thất bại.';
          this.isLoading = false;
          this.cdr.detectChanges();
       }
    });
  }

  deleteShift(id: number): void {
    if (confirm('Bạn có chắc chắn muốn hủy đăng ký ca làm này không?')) {
      this.isLoading = true;
      this.employeeShiftService.deleteEmployeeShift(id).pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })).subscribe({
        next: (res) => {
          alert(res.message || 'Hủy ca làm thành công.');
          this.loadMyShifts();
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hủy ca làm thất bại.';
          this.cdr.detectChanges();
        }
      });
    }
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

  getTodayString(): string {
    return new Date().toISOString().slice(0, 10);
  }
}