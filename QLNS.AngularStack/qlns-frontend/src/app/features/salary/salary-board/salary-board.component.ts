import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalaryItem, SalaryService } from '../services/salary.service';

@Component({
  selector: 'app-salary-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './salary-board.component.html',
  styleUrls: ['./salary-board.component.css']
})
export class SalaryBoardComponent implements OnInit {
  private salaryService = inject(SalaryService);
  private cdr = inject(ChangeDetectorRef);

  salaries: SalaryItem[] = [];
  selectedSalary: SalaryItem | null = null;

  selectedMonth = new Date().getMonth() + 1;
  selectedYear = new Date().getFullYear();

  isLoading = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadSalaries();
  }

  loadSalaries(): void {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.salaryService.getSalaries(this.selectedMonth, this.selectedYear).subscribe({
      next: (data) => {
        this.salaries = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Không thể tải dữ liệu bảng lương.';
        this.isLoading = false;
      }
    });
  }

  calculateSalaries(): void {
    const confirmed = confirm(
      `Bạn có chắc muốn tính lại lương tháng ${this.selectedMonth}/${this.selectedYear}?`
    );

    if (!confirmed) {
      return;
    }

    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.salaryService.calculateSalaries(this.selectedMonth, this.selectedYear).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Tính lương thành công.';
        this.loadSalaries();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Tính lương thất bại.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  clearSalaries(): void {
    const confirmed = confirm(
      `Bạn có chắc muốn xóa toàn bộ dữ liệu bảng lương tháng ${this.selectedMonth}/${this.selectedYear}?`
    );

    if (!confirmed) return;

    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.salaryService.clearSalaries(this.selectedMonth, this.selectedYear).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Xóa dữ liệu thành công.';
        this.loadSalaries();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Xóa dữ liệu thất bại.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  exportExcel(): void {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.salaryService.exportExcel(this.selectedMonth, this.selectedYear).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');

        a.href = url;
        a.download = `BangLuong_${this.selectedMonth}_${this.selectedYear}.xlsx`;
        a.click();

        window.URL.revokeObjectURL(url);

        this.successMessage = 'Xuất bảng lương Excel thành công.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Xuất bảng lương Excel thất bại.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  bulkSendEmail(): void {
    const confirmed = confirm(
      `Bạn có chắc muốn gửi email bảng lương tháng ${this.selectedMonth}/${this.selectedYear} cho toàn bộ nhân viên?`
    );

    if (!confirmed) {
      return;
    }

    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.salaryService.bulkSendEmail(this.selectedMonth, this.selectedYear).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Gửi email thành công.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Gửi email thất bại.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openDetail(item: SalaryItem): void {
    this.selectedSalary = item;
  }

  closeDetail(): void {
    this.selectedSalary = null;
  }

  get totalEmployees(): number {
    return this.salaries.length;
  }

  get totalBaseSalary(): number {
    return this.salaries.reduce((sum, item) => sum + item.baseSalary, 0);
  }

  get totalAllowance(): number {
    return this.salaries.reduce((sum, item) => sum + item.allowance, 0);
  }

  get totalDeduction(): number {
    return this.salaries.reduce((sum, item) => sum + item.deduction, 0);
  }

  get totalSalary(): number {
    return this.salaries.reduce((sum, item) => sum + item.totalSalary, 0);
  }
}