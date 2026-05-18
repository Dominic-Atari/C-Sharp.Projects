import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ModalController } from '@ionic/angular';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-subject-topic-picker',
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule],
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Choose subject & topic</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="close()">Close</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <div class="picker-container">
        <div *ngIf="loading" class="loading">Loading…</div>
        <div *ngIf="!loading">
          <p *ngIf="stageName" class="muted">Stage: {{ stageName }}</p>
          <ion-list>
            <ion-list-header>Subjects</ion-list-header>
            <ion-searchbar [(ngModel)]="filterTerm" placeholder="Filter subjects" debounce="200"></ion-searchbar>
            <ion-item *ngFor="let s of filteredSubjects()" (click)="selectSubject(s)" [class.selected]="s.subjectId === selectedSubjectId">
              <ion-label>
                <h3>{{ s.name }}</h3>
                <p class="muted">Stage: {{ s.stage }}</p>
              </ion-label>
              <ion-note slot="end" class="subject-stage">{{ s.stage }}</ion-note>
            </ion-item>
            <ion-item *ngIf="filteredSubjects().length === 0">No subjects found</ion-item>
          </ion-list>

          <div *ngIf="topics && topics.length" class="topics-area">
            <ion-list>
              <ion-list-header>Topics</ion-list-header>
              <ion-item *ngFor="let t of topics" (click)="openTopic(t)">
                <ion-label>
                  <h3>{{ t.subtopic }}</h3>
                  <p class="muted">{{ t.name ?? '' }}</p>
                </ion-label>
                <ion-icon name="caret-forward-outline" slot="end"></ion-icon>
              </ion-item>
            </ion-list>
          </div>
          <div *ngIf="!topics || topics.length === 0" class="no-topics muted">No topics found</div>
        </div>
      </div>
    </ion-content>
  `,
  styles: [`
    .picker-container { padding: 0.5rem 1rem; }
    .loading { text-align:center; padding:1rem; }
    .muted { color: var(--ion-color-medium); font-size:0.9rem; }
    ion-searchbar { margin: 0.5rem 0; }
    ion-item.selected { background: rgba(var(--ion-color-primary-rgb), 0.06); border-left: 3px solid var(--ion-color-primary); }
    .subject-stage { font-size: 0.8rem; color: var(--ion-color-medium); }
    .topics-area { margin-top: 0.5rem; }
    .no-topics { padding: 0.75rem 0; text-align:center; }
  `]
})
export class SubjectTopicPickerComponent implements OnInit {
  @Input() schoolId?: string | null;
  @Input() stageId?: string | null;
  @Input() stageName?: string | null;

  subjects: Array<any> = [];
  topics: Array<any> = [];
  loading = true;
  filterTerm: string = '';
  selectedSubjectId?: string | null;

  constructor(private api: ApiService, private modalCtrl: ModalController, private router: Router) {}

  ngOnInit(): void {
    if (!this.schoolId) { this.loading = false; return; }
    this.api.getSubjects(this.schoolId).subscribe({ next: subs => {
      let list = subs ?? [];
      // If a stageName is provided, try to filter subjects by numeric label or matching string
      if (this.stageName) {
        const num = parseInt(String(this.stageName), 10);
        if (!isNaN(num)) {
          list = list.filter((ss: any) => Number(ss.stage) === num);
        } else {
          const sLower = String(this.stageName).toLowerCase();
          list = list.filter((ss: any) => String(ss.name).toLowerCase() === sLower || String(ss.stage) === this.stageName);
        }
      }
      this.subjects = list;
      this.loading = false;
    }, error: () => { this.subjects = []; this.loading = false; }});
  }

  async selectSubject(s: any) {
    if (!this.schoolId || !s) return;
    this.selectedSubjectId = s.subjectId;
    this.topics = [];
    try {
      this.topics = (await firstValueFrom(this.api.getTopics(this.schoolId, s.subjectId))) ?? [];
    } catch (e) {
      console.warn('Failed to load topics', e);
      this.topics = [];
    }
  }

  filteredSubjects(): Array<any> {
    const f = (this.filterTerm || '').trim().toLowerCase();
    if (!f) return this.subjects;
    return this.subjects.filter((s: any) => String(s.name).toLowerCase().includes(f) || String(s.stage).toLowerCase().includes(f));
  }

  openTopic(t: any) {
    if (!t || !t.topicId || !this.subjects) return;
    // navigate to topic performance page
    const subjectId = (this.subjects.find((s: any) => (this.topics || []).some((x: any) => x.topicId === t.topicId)) || {}).subjectId ?? null;
    if (!subjectId) {
      // fallback: try to derive from topic object
      this.modalCtrl.dismiss();
      return;
    }
    this.modalCtrl.dismiss().then(() => this.router.navigateByUrl(`/subjects/${subjectId}/topics/${t.topicId}/performance`));
  }

  close() { this.modalCtrl.dismiss(); }
}
