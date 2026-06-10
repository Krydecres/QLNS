import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { DepartmentService } from '../../../core/services/department.service';
import { Department, Employee } from '../../../core/models/models';

@Component({
  selector: 'app-department-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './department-details.component.html'
})
export class DepartmentDetailsComponent implements OnInit {
  department: Department | null = null;
  employees: Employee[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private departmentService: DepartmentService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.departmentService.getDepartment(+id).subscribe({
        next: (data) => {
          this.department = data.department;
          this.employees = data.employees;
          this.cdr.detectChanges();
        },
        error: (err) => console.error(err)
      });
    }
  }
}
