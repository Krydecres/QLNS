import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TimekeepingService, TimekeepingDto } from '../services/timekeeping.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-attendance-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-board.component.html',
  styleUrls: ['./attendance-board.component.css']
})
export class AttendanceBoardComponent implements OnInit {
  timekeepings: TimekeepingDto[] = [];
  todayRecord: TimekeepingDto | undefined;
  todayDate: string = new Date().toLocaleDateString('vi-VN');
  
  startDate: string = '';
  endDate: string = '';

  successMessage: string | null = null;
  errorMessage: string | null = null;

  private timekeepingService = inject(TimekeepingService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    const username = this.authService.username();
    if (username) {
      this.timekeepingService.getMyAttendance(username, this.startDate, this.endDate).subscribe({
        next: (res) => {
          this.timekeepings = res;
          // Find today's record based on server local time string usually 'YYYY-MM-DD'
          // We will just filter using the Date string
          const todayStr = new Date().toLocaleDateString('en-CA'); // Gets YYYY-MM-DD in local timezone
          this.todayRecord = this.timekeepings.find(t => t.date.startsWith(todayStr));
          this.cdr.detectChanges();
        },
        error: (err) => console.error(err)
      });
    }
  }

  applyFilter(): void {
    this.loadData();
  }

  clearFilter(): void {
    this.startDate = '';
    this.endDate = '';
    this.loadData();
  }

  checkIn(): void {
    const username = this.authService.username();
    if (username) {
      this.timekeepingService.checkIn(username).subscribe({
        next: (res) => {
          this.showSuccess(res.message);
          this.loadData();
        },
        error: (err) => {
          this.showError(err.error?.message || 'Lỗi khi check-in');
        }
      });
    }
  }

  checkOut(): void {
    const username = this.authService.username();
    if (username) {
      this.timekeepingService.checkOut(username).subscribe({
        next: (res) => {
          this.showSuccess(res.message);
          this.loadData();
        },
        error: (err) => {
          this.showError(err.error?.message || 'Lỗi khi check-out');
        }
      });
    }
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
