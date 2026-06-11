import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LeaveService } from '../services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { DayOffItem, MyDaysOffResponse } from '../models/day-off-item.model';

@Component({
  selector: 'app-my-days-off',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './my-days-off.component.html'
})
export class MyDaysOffComponent implements OnInit {
  upcomingDays: DayOffItem[] = [];
  pastDays: DayOffItem[] = [];
  currentYear: number = new Date().getFullYear();
  years: number[] = [];
  totalHolidays: number = 0;
  totalLeaves: number = 0;
  isLoading: boolean = false;

  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    const current = new Date().getFullYear();
    // Tạo list 5 năm: năm sau, năm nay, và 3 năm trước giống logic for loop MVC (currentYear + 1 to currentYear - 3)
    for (let i = current + 1; i >= current - 3; i--) {
      this.years.push(i);
    }
    this.loadData();
  }

  loadData(): void {
    const username = this.authService.username();
    if (username) {
      this.isLoading = true;
      this.cdr.detectChanges();
      this.leaveService.getMyDaysOff(username, this.currentYear).subscribe({
        next: (data) => {
          this.upcomingDays = data.upcomingDays;
          this.pastDays = data.pastDays;
          this.currentYear = data.currentYear;
          this.totalHolidays = data.totalHolidays;
          this.totalLeaves = data.totalLeaves;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error loading my days off', err);
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  onYearChange(): void {
    this.loadData();
  }

  getDaysLeft(dateStr: string): number {
    // Parse as local date by splitting to avoid UTC offset shift
    const [y, m, d] = dateStr.substring(0, 10).split('-').map(Number);
    const itemDate = new Date(y, m - 1, d);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return Math.floor((itemDate.getTime() - today.getTime()) / (1000 * 3600 * 24));
  }

  getDurationDays(startDateStr: string, endDateStr: string | undefined): number {
    if (!endDateStr) return 1;
    const [sy, sm, sd] = startDateStr.substring(0, 10).split('-').map(Number);
    const [ey, em, ed] = endDateStr.substring(0, 10).split('-').map(Number);
    const start = new Date(sy, sm - 1, sd);
    const end = new Date(ey, em - 1, ed);
    return Math.floor((end.getTime() - start.getTime()) / (1000 * 3600 * 24)) + 1;
  }

  // Lấy ngày (dd) từ chuỗi ngày, parse local để tránh lệch timezone
  getDay(dateStr: string): string {
    const day = parseInt(dateStr.substring(8, 10), 10);
    return day.toString();
  }

  // Lấy tháng (M) từ chuỗi ngày
  getMonth(dateStr: string): string {
    const month = parseInt(dateStr.substring(5, 7), 10);
    return month.toString();
  }

  // Format ngày thành dd/MM/yyyy
  formatDate(dateStr: string): string {
    const d = dateStr.substring(8, 10);
    const m = dateStr.substring(5, 7);
    const y = dateStr.substring(0, 4);
    return `${d}/${m}/${y}`;
  }
}
