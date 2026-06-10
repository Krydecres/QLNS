import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmployeeService } from '../../../core/services/employee.service';
import { ProfileUpdateRequest } from '../../../core/models/models';

@Component({
  selector: 'app-pending-updates',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pending-updates.component.html'
})
export class PendingUpdatesComponent implements OnInit {
  requests: ProfileUpdateRequest[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(private employeeService: EmployeeService) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.employeeService.getPendingUpdates().subscribe({
      next: (data) => {
        this.requests = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  approve(id: number, isApproved: boolean): void {
    const action = isApproved ? 'duyệt' : 'từ chối';
    if (confirm(`Bạn có chắc chắn muốn ${action} yêu cầu này?`)) {
      this.employeeService.approveUpdate(id, isApproved).subscribe({
        next: () => {
          alert(`Đã ${action} yêu cầu thành công.`);
          this.loadRequests();
        },
        error: (err) => console.error(err)
      });
    }
  }
}
