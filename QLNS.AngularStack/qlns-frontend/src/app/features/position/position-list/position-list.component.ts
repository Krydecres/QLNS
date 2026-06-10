import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PositionService } from '../../../core/services/position.service';
import { Position } from '../../../core/models/models';

@Component({
  selector: 'app-position-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './position-list.component.html'
})
export class PositionListComponent implements OnInit {
  positions: Position[] = [];
  private cdr = inject(ChangeDetectorRef);

  constructor(private positionService: PositionService) {}

  ngOnInit(): void {
    this.loadPositions();
  }

  loadPositions(): void {
    this.positionService.getPositions().subscribe({
      next: (data) => {
        this.positions = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  deletePosition(id: number, name: string): void {
    if (confirm(`Bạn có chắc chắn muốn xóa chức vụ ${name}?`)) {
      this.positionService.deletePosition(id).subscribe({
        next: () => {
          this.loadPositions();
          alert('Đã xóa chức vụ thành công.');
        },
        error: (err) => console.error(err)
      });
    }
  }
}
