import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { PositionService } from '../../../core/services/position.service';
import { Position, Employee } from '../../../core/models/models';

@Component({
  selector: 'app-position-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './position-details.component.html'
})
export class PositionDetailsComponent implements OnInit {
  position: Position | null = null;
  employees: Employee[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private positionService: PositionService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.positionService.getPosition(+id).subscribe({
        next: (data) => {
          this.position = data.position;
          this.employees = data.employees;
          this.cdr.detectChanges();
        },
        error: (err) => console.error(err)
      });
    }
  }
}
