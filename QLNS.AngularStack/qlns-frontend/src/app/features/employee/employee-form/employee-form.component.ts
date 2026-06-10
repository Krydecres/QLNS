import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { EmployeeService } from '../../../core/services/employee.service';
import { DepartmentService } from '../../../core/services/department.service';
import { PositionService } from '../../../core/services/position.service';
import { Department, Position } from '../../../core/models/models';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './employee-form.component.html'
})
export class EmployeeFormComponent implements OnInit {
  employeeForm!: FormGroup;
  isEditMode = false;
  employeeId!: number;
  departments: Department[] = [];
  positions: Position[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private fb: FormBuilder,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private positionService: PositionService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.employeeForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      dateOfBirth: [''],
      departmentId: [''],
      positionId: ['']
    });

    this.loadDropdowns();

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.employeeId = +id;
        this.loadEmployee();
      }
    });
  }

  loadDropdowns(): void {
    forkJoin({
      departments: this.departmentService.getDepartments(),
      positions: this.positionService.getPositions()
    }).subscribe({
      next: (result) => {
        this.departments = result.departments;
        this.positions = result.positions;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  loadEmployee(): void {
    this.employeeService.getEmployee(this.employeeId).subscribe({
      next: (data) => {
        // Format date string to YYYY-MM-DD for the input type="date"
        let dob = '';
        if (data.dateOfBirth) {
            dob = new Date(data.dateOfBirth).toISOString().split('T')[0];
        }

        this.employeeForm.patchValue({
          fullName: data.fullName,
          email: data.email,
          phoneNumber: data.phoneNumber,
          dateOfBirth: dob,
          departmentId: data.departmentId || '',
          positionId: data.positionId || ''
        });
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  onSubmit(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    const formValues = this.employeeForm.value;
    const employeeData = {
      id: this.isEditMode ? this.employeeId : 0,
      fullName: formValues.fullName,
      email: formValues.email,
      phoneNumber: formValues.phoneNumber,
      dateOfBirth: formValues.dateOfBirth || null,
      departmentId: formValues.departmentId ? +formValues.departmentId : null,
      positionId: formValues.positionId ? +formValues.positionId : null
    };

    if (this.isEditMode) {
      this.employeeService.updateEmployee(this.employeeId, employeeData as any).subscribe({
        next: () => {
          alert('Cập nhật nhân viên thành công.');
          this.router.navigate(['/employees']);
        },
        error: (err) => console.error(err)
      });
    } else {
      this.employeeService.createEmployee(employeeData as any).subscribe({
        next: () => {
          alert('Thêm mới nhân viên thành công.');
          this.router.navigate(['/employees']);
        },
        error: (err) => console.error(err)
      });
    }
  }
}
