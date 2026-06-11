import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LeaveService } from '../services/leave.service';
import { LeaveRequest } from '../models/leave-request.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-my-leaves',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-leaves.component.html',
  styleUrls: ['./my-leaves.component.css']
})
export class MyLeavesComponent implements OnInit {
  leaves: LeaveRequest[] = [];
  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadMyLeaves();
  }

  loadMyLeaves(): void {
    const username = this.authService.username();
    if (username) {
      this.leaveService.getMyLeaves(username).subscribe({
        next: (data: LeaveRequest[]) => {
          this.leaves = data;
          this.cdr.detectChanges();
        },
        error: (err: any) => console.error('Error fetching my leaves', err)
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
