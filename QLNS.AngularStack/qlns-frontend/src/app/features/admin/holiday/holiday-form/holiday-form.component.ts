import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HolidayService } from '../../../../core/services/holiday.service';
import { Holiday } from '../../../../core/models/holiday.model';

@Component({
  selector: 'app-holiday-form',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './holiday-form.component.html'
})
export class HolidayFormComponent implements OnInit {
  holidayForm!: FormGroup;
  isEditMode = false;
  holidayId!: number;
  errorMessage: string | null = null;

  private fb = inject(FormBuilder);
  private holidayService = inject(HolidayService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.holidayForm = this.fb.group({
      name: ['', Validators.required],
      month: [1, [Validators.required, Validators.min(1), Validators.max(12)]],
      day: [1, [Validators.required, Validators.min(1), Validators.max(31)]],
      description: ['']
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.holidayId = +id;
        this.loadHoliday();
      }
    });
  }

  loadHoliday(): void {
    this.holidayService.getHoliday(this.holidayId).subscribe({
      next: (data) => {
        this.holidayForm.patchValue({
          name: data.name,
          month: data.month,
          day: data.day,
          description: data.description
        });
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading holiday', err)
    });
  }

  onSubmit(): void {
    if (this.holidayForm.invalid) {
      this.holidayForm.markAllAsTouched();
      return;
    }

    const formValues = this.holidayForm.value;
    const holidayData: Holiday = {
      id: this.isEditMode ? this.holidayId : 0,
      name: formValues.name,
      month: formValues.month,
      day: formValues.day,
      description: formValues.description
    };

    if (this.isEditMode) {
      this.holidayService.updateHoliday(this.holidayId, holidayData).subscribe({
        next: () => {
          alert('Cập nhật ngày lễ thành công.');
          this.router.navigate(['/holidays']);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Có lỗi xảy ra.';
          this.cdr.detectChanges();
        }
      });
    } else {
      this.holidayService.createHoliday(holidayData).subscribe({
        next: () => {
          alert('Thêm mới ngày lễ thành công.');
          this.router.navigate(['/holidays']);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Có lỗi xảy ra.';
          this.cdr.detectChanges();
        }
      });
    }
  }
}
