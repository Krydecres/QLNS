import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { LeaveService } from '../services/leave.service';
import { LeaveRequestCreateDto } from '../models/leave-request.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-leave-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './leave-form.component.html',
  styleUrls: ['./leave-form.component.css']
})
export class LeaveFormComponent {
  model: LeaveRequestCreateDto = {
    startDate: '',
    endDate: '',
    reason: ''
  };
  errorMessage: string = '';

  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private router = inject(Router);

  onSubmit(): void {
    if (!this.model.startDate || !this.model.endDate || !this.model.reason) {
      this.errorMessage = 'Vui lòng nhập đầy đủ thông tin.';
      return;
    }

    if (new Date(this.model.endDate) < new Date(this.model.startDate)) {
      this.errorMessage = 'Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.';
      return;
    }

    const username = this.authService.username();
    if (username) {
      this.leaveService.createLeaveRequest(username, this.model).subscribe({
        next: () => {
          this.router.navigate(['/leave/my-leaves']);
        },
        error: (err: any) => {
          this.errorMessage = err.error?.message || 'Có lỗi xảy ra khi gửi đơn xin nghỉ.';
        }
      });
    }
  }
}
