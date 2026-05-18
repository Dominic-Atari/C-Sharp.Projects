import { Component, OnInit } from '@angular/core';

import { IonicModule } from '@ionic/angular';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ModalController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ChatPerformanceComponent } from '../../components/chat-performance/chat-performance.component';
import { StagePerformanceChartComponent } from '../../components/stage-performance-chart/stage-performance-chart.component';

@Component({
    selector: 'app-stage-performance',
    imports: [IonicModule, ChatPerformanceComponent, StagePerformanceChartComponent],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/dashboard"></ion-back-button>
        </ion-buttons>
        <ion-title>Level Performance</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      @if (loaded) {
        @if (students && students.length) {
          <ion-segment value="chart" (ionChange)="viewMode = $any($event.detail).value">
            <ion-segment-button value="chart">
              <ion-label>Chart</ion-label>
            </ion-segment-button>
            <ion-segment-button value="chat">
              <ion-label>Chat</ion-label>
            </ion-segment-button>
          </ion-segment>
          <div style="margin-top:0.8rem"></div>
          @if (viewMode === 'chart') {
            <div>
              <app-stage-performance-chart [students]="students"></app-stage-performance-chart>
            </div>
          }
          @if (viewMode === 'chat') {
            <div>
              <app-chat-performance [schoolId]="schoolId" [students]="students"></app-chat-performance>
            </div>
          }
        } @else {
          <div class="loading">No students assigned to this level.</div>
          @if (unassigned && unassigned.length) {
            <div style="padding:1rem">
              <p class="muted">Unassigned students — assign to this level:</p>
              <ion-list>
                @for (s of unassigned; track s) {
                  <ion-item>
                    <ion-label>{{ s.firstName || s.username || s.userId }}</ion-label>
                    <ion-button size="small" fill="clear" (click)="openTopicPerformanceForStudent(s.userId); $event.stopPropagation()">Topics</ion-button>
                    <ion-button size="small" (click)="assignStudentToStage(s.userId); $event.stopPropagation()">Assign</ion-button>
                  </ion-item>
                }
              </ion-list>
            </div>
          }
        }
      } @else {
        <div class="loading">Loading…</div>
      }
    </ion-content>
    `,
    styles: [`.loading { padding: 2rem; text-align: center; color: var(--ion-color-medium); }`]
})
export class StagePerformancePage implements OnInit {
  stageId?: string | null;
  schoolId?: string | null;
  students: Array<any> = [];
  unassigned: Array<any> = [];
  loaded = false;
  viewMode: 'chart' | 'chat' = 'chart';

  constructor(private route: ActivatedRoute, private api: ApiService, private router: Router, private modalCtrl: ModalController) {}

  ngOnInit(): void {
    this.stageId = this.route.snapshot.paramMap.get('stageId');
    const auth = this.api.loadAuth?.() ?? null;
    this.schoolId = auth?.schoolId ?? null;
    if (!this.schoolId) {
      // nothing to show; go back
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 20);
      return;
    }
    this.api.getStudents(this.schoolId).subscribe({ next: st => {
      const all = st ?? [];
      this.students = all.filter((s: any) => (s as any).stageId === this.stageId);
      this.unassigned = all.filter((s: any) => !s.stageId);
      try { console.debug('StagePerformance: students for stage', this.stageId, this.students, 'unassigned', this.unassigned); } catch {}
      (window as any).__debugStage = { stageId: this.stageId, students: this.students, unassigned: this.unassigned };
      this.loaded = true;
    }, error: () => { this.students = []; this.unassigned = []; this.loaded = true; } });
  }

  async assignStudentToStage(userId: string): Promise<void> {
    if (!this.schoolId || !userId || !this.stageId) return;
    try {
      await firstValueFrom(this.api.updateStudent(this.schoolId, userId, { stageId: this.stageId }));
      // refresh lists
      const all = await firstValueFrom(this.api.getStudents(this.schoolId));
      this.students = (all ?? []).filter((s: any) => (s as any).stageId === this.stageId);
      this.unassigned = (all ?? []).filter((s: any) => !s.stageId);
    } catch (e) {
      console.warn('Failed to assign student to stage', e);
    }
  }

  async openTopicPerformanceForStudent(userId: string): Promise<void> {
    if (!this.schoolId || !userId) return;
    try {
      const m = await this.modalCtrl.create({
        component: (await import('../../components/topic-performance/topic-performance.component')).TopicPerformanceComponent,
        componentProps: { schoolId: this.schoolId ?? null, userId },
        cssClass: 'topic-performance-modal'
      });
      await m.present();
    } catch (e) {
      console.warn('Failed to open topic performance modal for student', e);
    }
  }
}
