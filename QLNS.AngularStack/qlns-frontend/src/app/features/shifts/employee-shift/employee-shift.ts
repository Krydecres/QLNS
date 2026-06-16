import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  EmployeeOption,
  EmployeeShiftItem,
  EmployeeShiftRequest,
  EmployeeShiftService,
  ShiftOption
} from '../services/employee-shift.service';

@Component({
  selector: 'app-employee-shift',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-shift.html',
  styleUrl: './employee-shift.css',
})
export class EmployeeShift implements OnInit {
  private employeeShiftService = inject(EmployeeShiftService);
  private cdr = inject(ChangeDetectorRef);

  employeeShifts: EmployeeShiftItem[] = [];
  employees: EmployeeOption[] = [];
  shifts: ShiftOption[] = [];

  startDate = '';
  endDate = '';
  selectedEmployeeId = 0;
  selectedShiftId = 0;

  editingId: number | null = null;

  form: EmployeeShiftRequest = this.getDefaultForm();

  isLoading = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadOptions();
    this.loadEmployeeShifts();
  }

  loadOptions(): void {
    this.employeeShiftService.getOptions().subscribe({
      next: (data) => {
        this.employees = data.employees;
        this.shifts = data.shifts;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Không thể tải danh sách nhân viên và ca làm.';
      }
    });
  }

  loadEmployeeShifts(): void {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.employeeShiftService
      .getEmployeeShifts(
        this.startDate,
        this.endDate,
        this.selectedEmployeeId,
        this.selectedShiftId
      )
      .subscribe({
        next: (data) => {
          this.employeeShifts = data;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Không thể tải danh sách phân ca.';
          this.isLoading = false;
        }
      });
  }

  saveEmployeeShift(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.form.employeeId) {
      this.errorMessage = 'Vui lòng chọn nhân viên.';
      return;
    }

    if (!this.form.shiftId) {
      this.errorMessage = 'Vui lòng chọn ca làm.';
      return;
    }

    if (!this.form.workDate) {
      this.errorMessage = 'Vui lòng chọn ngày làm việc.';
      return;
    }

    if (this.editingId) {
      this.employeeShiftService.updateEmployeeShift(this.editingId, this.form).subscribe({
        next: (res) => {
          this.successMessage = res.message || 'Cập nhật lịch phân ca thành công.';
          this.resetForm();
          this.loadEmployeeShifts();
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Cập nhật lịch phân ca thất bại.';
        }
      });

      return;
    }

    this.employeeShiftService.createEmployeeShift(this.form).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Gán ca cho nhân viên thành công.';
        this.resetForm();
        this.loadEmployeeShifts();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Gán ca cho nhân viên thất bại.';
      }
    });
  }

  editEmployeeShift(item: EmployeeShiftItem): void {
    this.editingId = item.id;

    this.form = {
      employeeId: item.employeeId,
      shiftId: item.shiftId,
      workDate: item.workDate,
      note: item.note || '',
      isActive: item.isActive
    };
  }

  deleteEmployeeShift(item: EmployeeShiftItem): void {
    const confirmed = confirm(
      `Bạn có chắc muốn ngưng áp dụng ca "${item.shiftName}" cho ${item.employeeName} không?`
    );

    if (!confirmed) {
      return;
    }

    this.employeeShiftService.deleteEmployeeShift(item.id).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Đã ngưng áp dụng lịch phân ca.';
        this.loadEmployeeShifts();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Không thể ngưng áp dụng lịch phân ca.';
      }
    });
  }

  resetForm(): void {
    this.editingId = null;
    this.form = this.getDefaultForm();
  }

  clearFilter(): void {
    this.startDate = '';
    this.endDate = '';
    this.selectedEmployeeId = 0;
    this.selectedShiftId = 0;
    this.loadEmployeeShifts();
  }

  getDefaultForm(): EmployeeShiftRequest {
    return {
      employeeId: 0,
      shiftId: 0,
      workDate: this.getTodayString(),
      note: '',
      isActive: true
    };
  }

  getTodayString(): string {
    return new Date().toISOString().slice(0, 10);
  }

  get activeCount(): number {
    return this.employeeShifts.filter(item => item.isActive).length;
  }

  get inactiveCount(): number {
    return this.employeeShifts.filter(item => !item.isActive).length;
  }
}