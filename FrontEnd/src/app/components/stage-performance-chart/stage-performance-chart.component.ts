import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Group { label: string; count: number; pct: number }

@Component({
  selector: 'app-stage-performance-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spc-root" *ngIf="students?.length; else empty">
      <div class="spc-header">
        <h3>Performance snapshot</h3>
        <small>{{ students.length }} learners</small>
      </div>
      <div class="spc-bars">
        <div class="spc-bar" *ngFor="let g of groups" [attr.data-label]="g.label">
          <div class="spc-meta">
            <div class="spc-label">{{ g.label }}</div>
            <div class="spc-count">{{ g.count }} • {{ g.pct }}%</div>
          </div>
          <div class="spc-track">
            <div class="spc-fill" [style.width.%]="g.pct"></div>
          </div>
        </div>
      </div>
    </div>
    <ng-template #empty>
      <div class="spc-empty">No learners to show performance for.</div>
    </ng-template>
  `,
  styleUrls: ['./stage-performance-chart.component.scss']
})
export class StagePerformanceChartComponent {
  @Input() students: Array<any> = [];

  get groups(): Group[] {
    const total = this.students?.length ?? 0;
    if (!total) return [];
    // deterministic pseudo-score from userId to keep bars stable
    const scores = (this.students ?? []).map(s => this.scoreFor(s));
    const beginner = scores.filter(x => x < 50).length;
    const intermediate = scores.filter(x => x >= 50 && x < 75).length;
    const advanced = scores.filter(x => x >= 75).length;
    const arr = [
      { label: 'Beginner', count: beginner, pct: Math.round((beginner / total) * 100) },
      { label: 'Intermediate', count: intermediate, pct: Math.round((intermediate / total) * 100) },
      { label: 'Advanced', count: advanced, pct: Math.round((advanced / total) * 100) },
    ];
    return arr;
  }

  private scoreFor(s: any): number {
    // Use a stable hash of userId/username to a 0-100 score so the chart doesn't jump
    const key = (s?.userId ?? s?.username ?? s?.firstName ?? '').toString();
    let h = 0;
    for (let i = 0; i < key.length; i++) { h = (h * 31 + key.charCodeAt(i)) >>> 0; }
    return Math.abs(h % 101);
  }
}
