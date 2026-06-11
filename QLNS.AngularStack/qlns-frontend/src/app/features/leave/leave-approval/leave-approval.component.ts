import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../services/leave.service';
import { LeaveRequest, LeaveRequestStatus } from '../models/leave-request.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-leave-approval',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-approval.component.html',
  styleUrls: ['./leave-approval.component.css']
})
export class LeaveApprovalComponent implements OnInit {
  requests: LeaveRequest[] = [];
  selectedRequest: LeaveRequest | null = null;
  approvalNote: string = '';
  actionStatus: LeaveRequestStatus = LeaveRequestStatus.Pending;

  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadApprovalList();
  }

  loadApprovalList(): void {
    this.leaveService.getApprovalList().subscribe({
      next: (data: LeaveRequest[]) => {
        this.requests = data;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error('Error fetching approval list', err)
    });
  }

  openApprovalModal(request: LeaveRequest, status: number): void {
    this.selectedRequest = request;
    this.actionStatus = status;
    this.approvalNote = '';
  }

  closeModal(): void {
    this.selectedRequest = null;
  }

  submitAction(): void {
    if (!this.selectedRequest) return;

    const adminUsername = this.authService.username();
    if (adminUsername) {
      this.leaveService.processRequest(this.selectedRequest.id, {
        status: this.actionStatus,
        note: this.approvalNote,
        adminUsername: adminUsername
      }).subscribe({
        next: () => {
          this.loadApprovalList();
          this.closeModal();
          this.cdr.detectChanges();
        },
        error: (err: any) => {
          console.error('Error processing request', err);
          alert(err.error?.message || 'Có lỗi xảy ra');
        }
      });
    }
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case 0: return 'bg-warning text-dark';
      case 1: return 'bg-success';
      case 2: return 'bg-danger';
      default: return 'bg-secondary';
    }
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Chờ duyệt';
      case 1: return 'Đã duyệt';
      case 2: return 'Từ chối';
      default: return 'Không xác định';
    }
  }
}
