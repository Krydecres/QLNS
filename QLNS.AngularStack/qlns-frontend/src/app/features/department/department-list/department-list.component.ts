import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DepartmentService } from '../../../core/services/department.service';
import { Department } from '../../../core/models/models';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './department-list.component.html'
})
export class DepartmentListComponent implements OnInit {
  departments: Department[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(private departmentService: DepartmentService) {}

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (data) => {
        this.departments = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  deleteDepartment(id: number, name: string): void {
    if (confirm(`Bạn có chắc chắn muốn xóa phòng ban ${name}?`)) {
      this.departmentService.deleteDepartment(id).subscribe({
        next: () => {
          this.loadDepartments();
          alert('Đã xóa phòng ban thành công.');
        },
        error: (err) => console.error(err)
      });
    }
  }
}
