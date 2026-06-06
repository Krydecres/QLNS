import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TimekeepingService, TimekeepingDto, UserDto } from '../services/timekeeping.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-manual-entry',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manual-entry.component.html',
  styleUrls: ['./manual-entry.component.css']
})
export class ManualEntryComponent implements OnInit {
  private timekeepingService = inject(TimekeepingService);
  private authService = inject(AuthService);

  users: UserDto[] = [];
  
  formData: Partial<TimekeepingDto> = {
    username: '',
    date: new Date().toISOString().split('T')[0],
    checkInTime: '',
    checkOutTime: '',
    status: 'Có mặt',
    note: ''
  };

  submitting = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit() {
    this.timekeepingService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
      },
      error: (err) => {
        console.error('Lỗi khi lấy danh sách nhân viên', err);
      }
    });
  }

  onSubmit() {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.formData.username) {
      this.errorMessage = 'Vui lòng chọn nhân viên';
      return;
    }

    if (!this.formData.date) {
      this.errorMessage = 'Vui lòng chọn ngày chấm công';
      return;
    }

    const selectedUser = this.users.find(u => u.username === this.formData.username);
    if (selectedUser) {
      this.formData.fullName = selectedUser.fullName;
    }

    this.submitting = true;
    
    // Ensure times are null if empty string
    const payload: TimekeepingDto = {
      username: this.formData.username!,
      fullName: this.formData.fullName || '',
      date: this.formData.date! + 'T00:00:00',
      checkInTime: this.formData.checkInTime || undefined,
      checkOutTime: this.formData.checkOutTime || undefined,
      status: this.formData.status,
      note: this.formData.note
    };

    this.timekeepingService.submitManualEntry(payload).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Lưu chấm công thành công!';
        this.submitting = false;
        // Reset some form data but keep date
        this.formData.username = '';
        this.formData.checkInTime = '';
        this.formData.checkOutTime = '';
        this.formData.note = '';
      },
      error: (err) => {
        this.errorMessage = 'Có lỗi xảy ra khi lưu. Vui lòng thử lại.';
        this.submitting = false;
        console.error(err);
      }
    });
  }
}
