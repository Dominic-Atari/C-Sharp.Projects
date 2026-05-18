import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { TopicPerformanceComponent } from '../../components/topic-performance/topic-performance.component';

@Component({
  selector: 'app-subject-topic-performance',
  standalone: true,
  imports: [CommonModule, IonicModule, TopicPerformanceComponent],
  template: `
  <ion-header>
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-back-button defaultHref="/teacher"></ion-back-button>
      </ion-buttons>
      <ion-title>Topic Performance</ion-title>
    </ion-toolbar>
  </ion-header>
  <ion-content>
    <ng-container *ngIf="loaded; else loading">
      <app-topic-performance [schoolId]="schoolId" [subjectId]="subjectId" [topicId]="topicId"></app-topic-performance>
    </ng-container>
    <ng-template #loading>
      <div class="loading">Loading…</div>
    </ng-template>
  </ion-content>
  `,
  styles: [`.loading { padding: 2rem; text-align: center; color: var(--ion-color-medium); }`]
})
export class SubjectTopicPerformancePage implements OnInit {
  subjectId?: string | null;
  topicId?: string | null;
  schoolId?: string | null;
  loaded = false;

  constructor(private route: ActivatedRoute, private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.subjectId = this.route.snapshot.paramMap.get('subjectId');
    this.topicId = this.route.snapshot.paramMap.get('topicId');
    const auth = this.api.loadAuth?.() ?? null;
    this.schoolId = auth?.schoolId ?? null;
    if (!this.schoolId || !this.subjectId || !this.topicId) {
      // missing context – go back
      setTimeout(() => this.router.navigateByUrl('/teacher'), 20);
      return;
    }
    // nothing else required: TopicPerformanceComponent will fetch topics for the subject and focus by topicId
    this.loaded = true;
  }
}
