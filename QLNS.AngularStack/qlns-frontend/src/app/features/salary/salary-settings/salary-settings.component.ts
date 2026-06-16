import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalarySettingsService, SalaryPosition, SalaryShift, SalarySettingsData } from '../services/salary-settings.service';

@Component({
  selector: 'app-salary-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './salary-settings.component.html'
})
export class SalarySettingsComponent implements OnInit {
  private salarySettingsService = inject(SalarySettingsService);
  private cdr = inject(ChangeDetectorRef);

  positions: SalaryPosition[] = [];
  shifts: SalaryShift[] = [];

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.salarySettingsService.getSettings().subscribe({
      next: (data: SalarySettingsData) => {
        this.positions = data.positions;
        this.shifts = data.shifts;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Không thể tải cấu hình bảng lương.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  updateDailyWage(position: SalaryPosition): void {
    const newWageStr = prompt(`Nhập lương ngày công mới cho chức vụ ${position.name}:`, position.dailyWage.toString());
    
    if (newWageStr === null) return;
    
    const newWage = parseFloat(newWageStr);
    if (isNaN(newWage) || newWage < 0) {
      this.errorMessage = 'Lương ngày công không hợp lệ. Phải là số dương.';
      return;
    }

    this.salarySettingsService.updatePositionDailyWage(position.id, newWage).subscribe({
      next: () => {
        position.dailyWage = newWage;
        this.successMessage = `Cập nhật lương ngày công cho ${position.name} thành công.`;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Cập nhật thất bại.';
        this.cdr.detectChanges();
      }
    });
  }

  updateWageMultiplier(shift: SalaryShift): void {
    const newMultiStr = prompt(`Nhập hệ số lương mới cho ca ${shift.name}:`, shift.wageMultiplier.toString());
    
    if (newMultiStr === null) return;
    
    const newMulti = parseFloat(newMultiStr);
    if (isNaN(newMulti) || newMulti <= 0) {
      this.errorMessage = 'Hệ số lương không hợp lệ. Phải lớn hơn 0.';
      return;
    }

    this.salarySettingsService.updateShiftWageMultiplier(shift.id, newMulti).subscribe({
      next: () => {
        shift.wageMultiplier = newMulti;
        this.successMessage = `Cập nhật hệ số lương cho ca ${shift.name} thành công.`;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Cập nhật thất bại.';
        this.cdr.detectChanges();
      }
    });
  }
}
