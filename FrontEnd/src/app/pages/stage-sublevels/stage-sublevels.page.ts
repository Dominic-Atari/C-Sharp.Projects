import { Component, OnInit } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonList, IonItem, IonLabel, IonButton } from '@ionic/angular/standalone';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-stage-sublevels',
    imports: [IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonList, IonItem, IonLabel, IonButton],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Level / Sublevels</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="page-shell">
      @if (stageName) {
        <ion-card>
          <ion-card-content>
            <h2>{{ stageName }}</h2>
            <p class="muted">{{ students.length }} students</p>
          </ion-card-content>
        </ion-card>
      }
    
      @if (students && students.length) {
        <ion-card>
          <ion-card-content>
            <h3>All students in this level</h3>
            <ion-list>
              @for (st of students; track st) {
                <ion-item (click)="viewStudent(st.userId)">
                  <ion-label>{{ st.firstName || st.username }} {{ st.lastName || '' }}</ion-label>
                </ion-item>
              }
            </ion-list>
          </ion-card-content>
        </ion-card>
      }
    
      @if (sublevels && sublevels.length) {
        <ion-card>
          <ion-card-content>
            <h3>Sublevels</h3>
            <ion-list>
              @for (s of sublevels; track s) {
                <ion-item (click)="selectSublevel(s.subLevelId)">
                  <ion-label>
                    <div>{{ s.name }}</div>
                    <div class="muted">Click to view students</div>
                  </ion-label>
                </ion-item>
              }
            </ion-list>
          </ion-card-content>
        </ion-card>
      }
    
      @if (selectedSublevel) {
        <ion-card>
          <ion-card-content>
            <h3>Students in {{ selectedSublevelName }}</h3>
            <ion-list>
              @for (st of studentsForSelected; track st) {
                <ion-item (click)="viewStudent(st.userId)">
                  <ion-label>{{ st.firstName || st.username }} {{ st.lastName || '' }}</ion-label>
                </ion-item>
              }
            </ion-list>
          </ion-card-content>
        </ion-card>
      }
    
      @if (!sublevels.length && !students.length) {
        <div class="muted">No sublevels or students found for this level.</div>
      }
    </ion-content>
    `,
    styleUrls: ['../create-subject/create-subject.page.scss']
})
export class StageSublevelsPage implements OnInit {
  stageId: string | null = null;
  stageName: string | null = null;
  sublevels: Array<{ subLevelId: string; name: string }> = [];
  students: Array<any> = [];
  selectedSublevel: string | null = null;
  selectedSublevelName: string | null = null;
  studentsForSelected: Array<any> = [];

  constructor(private route: ActivatedRoute, private api: ApiService, private router: Router) {}

  async ngOnInit(): Promise<void> {
    this.stageId = this.route.snapshot.paramMap.get('stageId');
    if (!this.stageId) return;
    const auth = this.api.loadAuth();
    const schoolId = auth?.schoolId;
    try {
      // set human-friendly stage name when possible
      if (schoolId) {
        try { const stages = await firstValueFrom(this.api.getStages(schoolId)); this.stageName = stages?.find(st => st.stageId === this.stageId)?.name ?? null; } catch {}
      }
      if (schoolId) {
        const subs = await firstValueFrom(this.api.getSubLevels(schoolId, this.stageId));
        this.sublevels = (subs || []).map(s => ({ subLevelId: s.subLevelId, name: s.name }));
        const students = await firstValueFrom(this.api.getStudentsForStage(schoolId, this.stageId));
        this.students = students || [];
      } else {
        this.sublevels = [];
        this.students = [];
      }
    } catch (err) { this.sublevels = []; this.students = []; }
  }

  selectSublevel(subLevelId: string): void {
    this.selectedSublevel = subLevelId;
    const s = this.sublevels.find(x => x.subLevelId === subLevelId);
    this.selectedSublevelName = s?.name ?? null;
    this.studentsForSelected = (this.students || []).filter(st => (st as any).subLevelId === subLevelId);
  }

  viewStudent(userId: string): void {
    if (!userId) return;
    void this.router.navigateByUrl(`/learner/${userId}`);
  }
}
