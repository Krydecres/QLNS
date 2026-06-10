import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { DepartmentService } from '../../../core/services/department.service';

@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './department-form.component.html'
})
export class DepartmentFormComponent implements OnInit {
  departmentForm!: FormGroup;
  isEditMode = false;
  departmentId!: number;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private fb: FormBuilder,
    private departmentService: DepartmentService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.departmentForm = this.fb.group({
      name: ['', Validators.required]
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.departmentId = +id;
        this.loadDepartment();
      }
    });
  }

  loadDepartment(): void {
    this.departmentService.getDepartment(this.departmentId).subscribe({
      next: (data) => {
        this.departmentForm.patchValue({
          name: data.department.name
        });
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  onSubmit(): void {
    if (this.departmentForm.invalid) {
      return;
    }

    const departmentData = {
      id: this.isEditMode ? this.departmentId : 0,
      ...this.departmentForm.value
    };

    if (this.isEditMode) {
      this.departmentService.updateDepartment(this.departmentId, departmentData).subscribe({
        next: () => {
          alert('Cập nhật phòng ban thành công.');
          this.router.navigate(['/departments']);
        },
        error: (err) => console.error(err)
      });
    } else {
      this.departmentService.createDepartment(departmentData).subscribe({
        next: () => {
          alert('Thêm mới phòng ban thành công.');
          this.router.navigate(['/departments']);
        },
        error: (err) => console.error(err)
      });
    }
  }
}
