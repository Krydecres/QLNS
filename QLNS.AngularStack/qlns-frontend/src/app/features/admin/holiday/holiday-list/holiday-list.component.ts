import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HolidayService } from '../../../../core/services/holiday.service';
import { Holiday } from '../../../../core/models/holiday.model';

@Component({
  selector: 'app-holiday-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './holiday-list.component.html'
})
export class HolidayListComponent implements OnInit {
  holidays: Holiday[] = [];
  currentYear: number = new Date().getFullYear();

  private holidayService = inject(HolidayService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadHolidays();
  }

  loadHolidays(): void {
    this.holidayService.getHolidays().subscribe({
      next: (data) => {
        this.holidays = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading holidays', err)
    });
  }

  deleteHoliday(id: number, name: string): void {
    if (confirm(`Bạn có chắc chắn muốn xóa ngày lễ: ${name}?`)) {
      this.holidayService.deleteHoliday(id).subscribe({
        next: () => {
          this.loadHolidays();
          alert('Đã xóa ngày lễ thành công.');
        },
        error: (err) => {
          console.error('Error deleting holiday', err);
          alert('Có lỗi xảy ra khi xóa ngày lễ.');
        }
      });
    }
  }

  getDateForThisYear(month: number, day: number): Date {
    const daysInMonth = new Date(this.currentYear, month, 0).getDate();
    const actualDay = day > daysInMonth ? daysInMonth : day;
    return new Date(this.currentYear, month - 1, actualDay);
  }

  isPast(month: number, day: number): boolean {
    const d = this.getDateForThisYear(month, day);
    d.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return d.getTime() < today.getTime();
  }

  isToday(month: number, day: number): boolean {
    const d = this.getDateForThisYear(month, day);
    d.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return d.getTime() === today.getTime();
  }
}
