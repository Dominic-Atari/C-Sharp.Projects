import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';
import { CreateNotePage } from '../../pages/create-note/create-note.page';

@Component({
  selector: 'app-student-learning',
  standalone: true,
  imports: [CommonModule, IonicModule],
  template: `
  <ion-header>
    <ion-toolbar>
      <ion-title>Study — {{ subjectName || 'Subject' }}</ion-title>
      <ion-buttons slot="end"><ion-button (click)="close()">Close</ion-button></ion-buttons>
    </ion-toolbar>
  </ion-header>
  <ion-content class="learning" fullscreen>
    <div class="subject-head">
      <h2>{{ subjectName }}</h2>
      <p class="hint">Swipe topics → or tap a topic to expand subtopics. Quick practice keeps sessions short and addictive.</p>
    </div>

    <div *ngIf="topics && topics.length" class="topics-strip">
      <div class="topic" *ngFor="let t of topics" (click)="selectTopic(t)" [class.active]="t.topicId===activeTopicId">
        <div class="topic-name">{{ t.name || t.subtopic }}</div>
        <div class="topic-meta">{{ t.subtopic || t.name }}</div>
        <div class="topic-score"><ion-progress-bar [value]="(t.score ?? 0)/100"></ion-progress-bar><small>{{ t.score ?? '?' }}%</small></div>
      </div>
    </div>

    <div *ngIf="activeTopic" class="subtopics">
      <h3>{{ activeTopic.name || activeTopic.subtopic }}</h3>
      <div class="sub-list">
        <div *ngFor="let s of subtopics" class="subcard">
          <div class="sub-title">{{ s.name || s.subtopic }}</div>
          <div class="sub-notes" *ngIf="s.notes">{{ s.notes | slice:0:120 }}{{ s.notes?.length > 120 ? '…' : '' }}</div>
          <div class="sub-actions">
            <ion-button size="small" (click)="startPractice(s)">Practice</ion-button>
            <ion-button fill="clear" size="small" (click)="openNotes(s)">Notes</ion-button>
            <div class="badge">{{ s.score ?? 75 }}%</div>
          </div>
        </div>
      </div>
    </div>

    <div *ngIf="!topics || !topics.length" class="empty">No topics available yet. Ask your teacher to add topics.</div>
  </ion-content>
  `,
  styles: [
    `:host { display:block; }
     .subject-head { padding:12px 16px; }
     .hint { color: var(--ion-color-medium); margin-top:4px; }
     .topics-strip { display:flex; gap:12px; padding:12px; overflow:auto; }
     .topic { min-width:180px; background:var(--ion-card-background); border-radius:10px; padding:12px; box-shadow: 0 1px 4px rgba(0,0,0,0.04); cursor:pointer; }
     .topic.active { outline: 3px solid rgba(0,150,136,0.12); }
     .topic-name { font-weight:700; margin-bottom:6px; }
     .topic-meta { color:var(--ion-color-medium); font-size:13px; margin-bottom:8px }
     .topic-score { display:flex; align-items:center; gap:8px }
     .subtopics { padding:12px; }
     .sub-list { display:grid; grid-template-columns: repeat(auto-fill,minmax(200px,1fr)); gap:12px }
     .subcard { padding:12px; border-radius:8px; background:var(--ion-background-color); box-shadow: 0 1px 3px rgba(0,0,0,0.04); }
     .sub-actions { display:flex; align-items:center; gap:8px; margin-top:10px }
     .badge { background:#eee; padding:4px 8px; border-radius:12px; font-weight:700; font-size:13px }
     .empty { padding:18px; color:var(--ion-color-medium) }
  `]
})
export class StudentLearningComponent implements OnInit {
  @Input() schoolId?: string | null;
  @Input() subjectId?: string | null;
  @Input() subjectName?: string | null;

  topics: Array<any> = [];
  subtopics: Array<any> = [];
  activeTopic: any = null;
  activeTopicId?: string | null;

  constructor(private api: ApiService, private modal: ModalController, private toast: ToastController) {}

  ngOnInit(): void {
    if (!this.schoolId || !this.subjectId) return;
    this.api.getTopics(this.schoolId, this.subjectId).subscribe({ next: (t: any) => {
      this.topics = (t || []).map((x: any, i: number) => ({ ...x, score: Math.round(45 + (i%5)*10) }));
      if (this.topics.length) this.selectTopic(this.topics[0]);
    }, error: () => { this.topics = []; } });
  }

  selectTopic(t: any) {
    this.activeTopic = t; this.activeTopicId = t.topicId;
    // derive subtopics from DB
    (async () => {
      try {
        const details = await firstValueFrom(this.api.getTopic(t.topicId));
        this.subtopics = (details?.subtopics ?? []).map((st: any) => ({ ...st, score: Math.round(55 + Math.random()*40) }));
      } catch (e) {
        this.subtopics = [];
      }
    })();
  }

  async startPractice(sub: any) {
    const t = await this.toast.create({ message: `Practice: ${sub.name ?? sub.subtopic}`, duration: 900, color: 'primary' });
    await t.present();
    sub.score = Math.min(100, (sub.score ?? 75) + 5);
  }

  async openNotes(sub: any) {
    if (!this.activeTopicId || !this.subjectId || !this.schoolId) return;
    try {
      const m = await this.modal.create({ component: CreateNotePage, componentProps: { topicId: this.activeTopicId, subjectId: this.subjectId, schoolId: this.schoolId, subTopicId: sub.subTopicId ?? null }, cssClass: 'full-screen-modal' });
      await m.present();
    } catch (e) {
      console.warn('Failed to open notes modal', e);
    }
  }

  close() { this.modal.dismiss(); }
}
