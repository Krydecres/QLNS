import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { EmployeeService } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';
import { Department, Employee } from '../../../core/models/models';

@Component({
  selector: 'app-my-department',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-department.component.html'
})
export class MyDepartmentComponent implements OnInit {
  department: Department | null = null;
  members: Employee[] = [];
  noDept = false;
  currentEmail = '';

  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadDepartment();
  }

  loadDepartment(): void {
    const username = this.authService.username();
    if (username) {
      this.currentEmail = username; // Since API matches by username or email
      this.employeeService.getMyDepartment(username).subscribe({
        next: (data) => {
          if (!data.department) {
            this.noDept = true;
          } else {
            this.department = data.department;
            this.members = data.members;
          }
          this.cdr.detectChanges();
        },
        error: (err) => console.error(err)
      });
    }
  }

  isMe(member: Employee): boolean {
    return member.email === this.currentEmail || member.email === this.authService.username(); // Or compare by ID if available, but username is what we have
  }
}
