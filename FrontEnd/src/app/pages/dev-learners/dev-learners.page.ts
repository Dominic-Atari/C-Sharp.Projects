import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-dev-learners',
  standalone: true,
  imports: [CommonModule, IonicModule],
  template: `
  <ion-header>
    <ion-toolbar>
      <ion-title>Dev: Learners Preview</ion-title>
      <ion-buttons slot="end"><ion-button (click)="refresh()">Refresh</ion-button></ion-buttons>
    </ion-toolbar>
  </ion-header>
  <ion-content>
    <div class="wrap">
      <p class="muted">Click a learner to open <strong>Learner Performance</strong> page.</p>
      <ion-list>
        <ion-item *ngFor="let s of students" (click)="openLearner(s)">
          <ion-label>
            <h3>{{ s.firstName || s.username || ('Learner ' + s.userId?.slice(0,6)) }}</h3>
            <p class="muted">{{ s.userId }}</p>
          </ion-label>
          <ion-button fill="clear" size="small" slot="end" (click)="openLearner(s); $event.stopPropagation()">Open</ion-button>
        </ion-item>
      </ion-list>
      <div *ngIf="students.length === 0" class="empty muted">No learners found for this school.</div>
    </div>
  </ion-content>
  `,
  styles: [`.wrap{padding:1rem} .muted{color:var(--ion-color-medium)} .empty{padding:1rem;text-align:center}`]
})
export class DevLearnersPage implements OnInit {
  students: Array<any> = [];

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    const auth = this.api.loadAuth?.() ?? null;
    const schoolId = auth?.schoolId ?? null;
    if (!schoolId) { this.students = []; return; }
    this.api.getStudents(schoolId).subscribe({ next: st => this.students = st ?? [], error: () => this.students = [] });
  }

  openLearner(s: any) {
    if (!s?.userId) return;
    this.router.navigateByUrl(`/learner/${s.userId}`);
  }
}
