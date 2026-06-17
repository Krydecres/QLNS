import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShiftItem, ShiftRequest, ShiftService } from '../services/shift.service';

@Component({
  selector: 'app-shift-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './shift-list.html',
  styleUrl: './shift-list.css',
})
export class ShiftList implements OnInit {
  private shiftService = inject(ShiftService);
  private cdr = inject(ChangeDetectorRef);

  shifts: ShiftItem[] = [];
  search = '';

  isLoading = false;
  successMessage = '';
  errorMessage = '';

  editingId: number | null = null;

  form: ShiftRequest = this.getDefaultForm();

  ngOnInit(): void {
    this.loadShifts();
  }

  loadShifts(): void {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.shiftService.getShifts(this.search).subscribe({
      next: (data) => {
        this.shifts = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Không thể tải danh sách ca làm.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  saveShift(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.form.name.trim()) {
      this.errorMessage = 'Tên ca làm không được để trống.';
      return;
    }

    if (this.form.startTime === this.form.endTime) {
      this.errorMessage = 'Giờ bắt đầu và giờ kết thúc không được trùng nhau.';
      return;
    }

    if (this.form.breakMinutes < 0 || this.form.breakMinutes > 240) {
      this.errorMessage = 'Thời gian nghỉ phải từ 0 đến 240 phút.';
      return;
    }

    if (this.form.wageMultiplier <= 0) {
      this.errorMessage = 'Hệ số lương phải lớn hơn 0.';
      return;
    }

    if (this.editingId) {
      this.shiftService.updateShift(this.editingId, this.form).subscribe({
        next: (res) => {
          this.successMessage = res.message || 'Cập nhật ca làm thành công.';
          this.resetForm();
          this.loadShifts();
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Cập nhật ca làm thất bại.';
        }
      });

      return;
    }

    this.shiftService.createShift(this.form).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Tạo ca làm thành công.';
        this.resetForm();
        this.loadShifts();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Tạo ca làm thất bại.';
      }
    });
  }

  editShift(item: ShiftItem): void {
    this.editingId = item.id;

    this.form = {
      name: item.name,
      startTime: item.startTime,
      endTime: item.endTime,
      breakMinutes: item.breakMinutes,
      description: item.description || '',
      wageMultiplier: item.wageMultiplier,
      isActive: item.isActive
    };
  }

  cancelEdit(): void {
    this.resetForm();
  }

  deleteShift(item: ShiftItem): void {
    const confirmed = confirm(
      `Bạn có chắc muốn ngưng sử dụng ca "${item.name}" không?`
    );

    if (!confirmed) {
      return;
    }

    this.shiftService.deleteShift(item.id).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Đã ngưng sử dụng ca làm.';
        this.loadShifts();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Không thể ngưng sử dụng ca làm.';
      }
    });
  }

  permanentDeleteShift(item: ShiftItem): void {
    const confirmed = confirm(
      `⚠️ XÓA VĨNH VIỄN ca "${item.name}"?\n\nHành động này KHÔNG THỂ hoàn tác. Ca làm sẽ bị xóa hoàn toàn khỏi hệ thống.`
    );

    if (!confirmed) {
      return;
    }

    this.shiftService.permanentDeleteShift(item.id).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Đã xóa ca làm thành công.';
        this.loadShifts();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Không thể xóa ca làm.';
      }
    });
  }


  resetForm(): void {
    this.editingId = null;
    this.form = this.getDefaultForm();
  }

  getDefaultForm(): ShiftRequest {
    return {
      name: '',
      startTime: '08:00',
      endTime: '17:00',
      breakMinutes: 60,
      description: '',
      wageMultiplier: 1,
      isActive: true
    };
  }

  get activeCount(): number {
    return this.shifts.filter(item => item.isActive).length;
  }

  get inactiveCount(): number {
    return this.shifts.filter(item => !item.isActive).length;
  }
}