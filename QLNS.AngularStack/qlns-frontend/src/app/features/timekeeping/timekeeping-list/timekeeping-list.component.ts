import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { TimekeepingService, TimekeepingDto, UserDto } from '../services/timekeeping.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-timekeeping-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './timekeeping-list.component.html',
  styleUrls: ['./timekeeping-list.component.css']
})
export class TimekeepingListComponent implements OnInit {
  timekeepings: TimekeepingDto[] = [];
  users: UserDto[] = [];
  startDate: string = '';
  endDate: string = '';
  selectedEmployeeUsername: string = '';
  
  successMessage: string | null = null;
  errorMessage: string | null = null;

  private timekeepingService = inject(TimekeepingService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    if (this.authService.role() !== 'Admin') {
      this.router.navigate(['/timekeeping/my-attendance']);
      return;
    }
    this.loadData();
    this.loadUsers();
  }

  loadData(): void {
    this.timekeepingService.getAllAttendance(this.startDate, this.endDate).subscribe({
      next: (res) => {
        this.timekeepings = res;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  loadUsers(): void {
    this.timekeepingService.getUsers().subscribe({
      next: (res) => this.users = res,
      error: (err) => console.error(err)
    });
  }

  applyFilter(): void {
    this.loadData();
  }

  clearFilter(): void {
    this.startDate = '';
    this.endDate = '';
    this.loadData();
  }

  checkInForEmployee(): void {
    if (!this.selectedEmployeeUsername) {
      this.showError('Vui lòng chọn nhân viên để chấm công.');
      return;
    }
    this.timekeepingService.checkIn(this.selectedEmployeeUsername).subscribe({
      next: (res) => {
        this.showSuccess(res.message);
        this.loadData();
      },
      error: (err) => {
        this.showError(err.error?.message || 'Có lỗi xảy ra.');
      }
    });
  }

  exportExcel(): void {
    this.timekeepingService.exportExcel(this.startDate, this.endDate).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'DuLieuChamCong.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Export failed', err)
    });
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    this.errorMessage = null;
    setTimeout(() => this.successMessage = null, 3000);
  }

  showError(msg: string) {
    this.errorMessage = msg;
    this.successMessage = null;
    setTimeout(() => this.errorMessage = null, 3000);
  }
}
