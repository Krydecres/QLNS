import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { PositionService } from '../../../core/services/position.service';

@Component({
  selector: 'app-position-form',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './position-form.component.html'
})
export class PositionFormComponent implements OnInit {
  positionForm!: FormGroup;
  isEditMode = false;
  positionId!: number;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private fb: FormBuilder,
    private positionService: PositionService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.positionForm = this.fb.group({
      name: ['', Validators.required]
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.positionId = +id;
        this.loadPosition();
      }
    });
  }

  loadPosition(): void {
    this.positionService.getPosition(this.positionId).subscribe({
      next: (data) => {
        this.positionForm.patchValue({
          name: data.position.name
        });
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  onSubmit(): void {
    if (this.positionForm.invalid) {
      return;
    }

    const positionData = {
      id: this.isEditMode ? this.positionId : 0,
      ...this.positionForm.value
    };

    if (this.isEditMode) {
      this.positionService.updatePosition(this.positionId, positionData).subscribe({
        next: () => {
          alert('Cập nhật chức vụ thành công.');
          this.router.navigate(['/positions']);
        },
        error: (err) => console.error(err)
      });
    } else {
      this.positionService.createPosition(positionData).subscribe({
        next: () => {
          alert('Thêm mới chức vụ thành công.');
          this.router.navigate(['/positions']);
        },
        error: (err) => console.error(err)
      });
    }
  }
}
