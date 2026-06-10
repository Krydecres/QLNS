import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { EmployeeService } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';
import { Employee } from '../../../core/models/models';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './my-profile.component.html'
})
export class MyProfileComponent implements OnInit {
  employee: Employee | null = null;
  authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  isEditing = false;
  updateForm!: FormGroup;

  constructor(
    private employeeService: EmployeeService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.updateForm = this.fb.group({
      newPhoneNumber: [''],
      newDateOfBirth: ['']
    });
    this.loadProfile();
  }

  loadProfile(): void {
    const username = this.authService.username();
    if (username) {
      this.employeeService.getMyProfile(username).subscribe({
        next: (data) => {
          this.employee = data;
          let dob = '';
          if (data.dateOfBirth) {
             dob = new Date(data.dateOfBirth).toISOString().split('T')[0];
          }
          this.updateForm.patchValue({
            newPhoneNumber: data.phoneNumber,
            newDateOfBirth: dob
          });
          this.cdr.detectChanges();
        },
        error: (err) => console.error(err)
      });
    }
  }

  toggleEdit(): void {
    this.isEditing = !this.isEditing;
  }

  onSubmit(): void {
    if (this.updateForm.invalid || !this.employee) return;
    
    const request = {
      employeeId: this.employee.id,
      newPhoneNumber: this.updateForm.value.newPhoneNumber,
      newDateOfBirth: this.updateForm.value.newDateOfBirth || null
    };

    this.employeeService.requestProfileUpdate(request).subscribe({
      next: () => {
        alert('Yêu cầu cập nhật thông tin đã được gửi đến Quản trị viên chờ phê duyệt.');
        this.isEditing = false;
      },
      error: (err) => console.error(err)
    });
  }
}
