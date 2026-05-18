import { Component, Input } from '@angular/core';

import { IonicModule, PopoverController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-student-performance',
    imports: [IonicModule],
    template: `
  <ion-card>
    <ion-card-header>
      <ion-card-title>{{ displayName }}</ion-card-title>
    </ion-card-header>
    <ion-card-content>
      <p><strong>Username:</strong> {{ student?.username }}</p>
      @if (student?.firstName || student?.lastName) {
        <p><strong>Full name:</strong> {{ student?.firstName }} {{ student?.lastName }}</p>
      }
      <div style="margin-top:8px">
        <p><strong>Performance</strong></p>
        <p class="muted">No performance metrics available yet.</p>
      </div>
      <div style="margin-top:12px">
        <ion-button expand="block" (click)="close()">Close</ion-button>
      </div>
    </ion-card-content>
  </ion-card>
  `
})
export class StudentPerformanceComponent {
  @Input() student!: { userId: string; username: string; firstName?: string | null; lastName?: string | null };
  @Input() schoolId?: string | null;

  displayName = '';

  constructor(private api: ApiService, private pop: PopoverController) {}

  ngOnInit(): void {
    this.displayName = `${this.student?.firstName ?? ''} ${this.student?.lastName ?? ''}`.trim() || this.student?.username || 'Student';
  }

  close(): void {
    this.pop.dismiss();
  }
}
