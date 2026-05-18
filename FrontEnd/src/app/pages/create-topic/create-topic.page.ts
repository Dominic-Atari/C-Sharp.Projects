import { Component, Input, OnInit } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { IonicModule, PopoverController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-create-topic',
    imports: [FormsModule, IonicModule],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Create Topic</ion-title>
        <ion-buttons slot="end">
          <ion-button fill="clear" (click)="dismiss()">✕</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      @if (!schoolId) {
        <div>No school selected.</div>
      }
    
      @if (schoolId) {
        <ion-item>
          <ion-label position="stacked">Subject</ion-label>
          <ion-select [(ngModel)]="selectedSubjectId" (ionChange)="loadTopicsForSelected()" placeholder="Select subject">
            @for (s of subjects; track s) {
              <ion-select-option [value]="s.subjectId">{{ s.name }}</ion-select-option>
            }
          </ion-select>
        </ion-item>
      }
    
      @if (topicsForSubject?.length) {
        <div style="margin-top:8px">
          <p style="margin:0 0 6px 0;font-weight:600">Existing topics for this subject</p>
          <div style="display:flex;flex-direction:column;gap:6px">
            @for (t of topicsForSubject; track t) {
              <ion-button size="small" fill="clear" (click)="chooseExisting(t)">• {{ t.subtopic }} <span style="color:var(--ion-color-medium);font-size:12px;margin-left:8px">{{ t.notes ? '(has notes)' : '' }}</span></ion-button>
            }
          </div>
        </div>
      }
    
      <div style="margin-top:8px">
        <p style="margin:0 0 6px 0;font-weight:600">Subtopics to create / edit</p>
        <div style="display:flex;flex-direction:column;gap:8px">
          @for (r of rows; track r; let idx = $index) {
            <div style="display:flex;flex-wrap:wrap;gap:8px;align-items:flex-start">
              <ion-item style="flex:1 1 320px; min-width:220px">
                <ion-label position="stacked">Subtopic</ion-label>
                <ion-input [(ngModel)]="r.subtopic" placeholder="Subtopic"></ion-input>
              </ion-item>
              <ion-item style="flex:1 1 320px; min-width:220px">
                <ion-label position="stacked">Notes (optional)</ion-label>
                <ion-textarea rows="3" [(ngModel)]="r.notes" placeholder="Add notes (optional)"></ion-textarea>
              </ion-item>
              <div style="display:flex;flex-direction:column;gap:6px;align-items:center">
                <ion-button size="small" color="danger" fill="clear" (click)="removeRow(idx)">Remove</ion-button>
              </div>
            </div>
          }
          <div>
            <ion-button size="small" fill="clear" (click)="addRow()">+ Add row</ion-button>
          </div>
        </div>
      </div>
    
      <div style="margin-top:12px;display:flex;gap:8px;">
        <ion-button expand="block" (click)="create()" [disabled]="!canCreate">Create All</ion-button>
        <ion-button expand="block" fill="clear" (click)="dismiss()">Cancel</ion-button>
      </div>
    </ion-content>
    `
})
export class CreateTopicPage implements OnInit {
  @Input() schoolId?: string | null;

  subjects: Array<{ subjectId: string; name: string }> = [];
  selectedSubjectId?: string | null;
  topicsForSubject: Array<{ topicId: string; subtopic: string; notes?: string | null }> = [];
  selectedTopicId?: string | null;
  // rows for batch create/update
  rows: Array<{ topicId?: string | null; subtopic: string; notes?: string | null }> = [{ subtopic: '', notes: '' }];

  private authUserId?: string | null;

  constructor(private api: ApiService, private popCtrl: PopoverController, private toastCtrl: ToastController) {}

  ngOnInit(): void {
    const auth = this.api.loadAuth();
    this.authUserId = auth?.userId ?? null;
    const teacherId = this.authUserId;
    const schoolId = auth?.schoolId ?? this.schoolId ?? null;
    if (schoolId && teacherId) {
      this.api.getTeacherSubjects(schoolId, teacherId).subscribe({
        next: subs => { this.subjects = subs ?? []; if (this.subjects.length === 1) this.selectedSubjectId = this.subjects[0].subjectId; this.loadTopicsForSelected(); },
        error: () => { this.subjects = []; }
      });
    } else if (this.schoolId) {
      this.api.getSubjects(this.schoolId).subscribe({ next: subs => this.subjects = subs ?? [], error: () => { this.subjects = []; } });
    }
  }

  loadTopicsForSelected(): void {
    const auth = this.api.loadAuth();
    const schoolId = auth?.schoolId ?? this.schoolId ?? null;
    if (!schoolId || !this.selectedSubjectId) { this.topicsForSubject = []; return; }
    this.api.getTopics(schoolId, this.selectedSubjectId).subscribe({ next: t => { this.topicsForSubject = t ?? []; }, error: () => { this.topicsForSubject = []; } });
  }

  get canCreate(): boolean {
    const auth = this.api.loadAuth();
    const schoolId = auth?.schoolId ?? this.schoolId ?? null;
    if (!schoolId || !this.selectedSubjectId) return false;
    return this.rows.some(r => r.subtopic && r.subtopic.trim().length > 0);
  }

  async create(): Promise<void> {
    const auth = this.api.loadAuth();
    const schoolId = auth?.schoolId ?? this.schoolId ?? null;
    if (!schoolId || !this.selectedSubjectId) return;

    // Batch create/update rows sequentially. Show per-row toasts and dispatch events.
    const results: Array<{ success: boolean; topicId?: string | null; error?: any; subtopic?: string }> = [];
    for (const r of this.rows) {
      const text = r.subtopic?.trim() ?? '';
      if (!text) { results.push({ success: false, error: 'empty', subtopic: '' }); continue; }
      try {
        if (r.topicId) {
          // update existing
          await firstValueToPromise(this.api.updateTopic(schoolId, this.selectedSubjectId, r.topicId, { subtopic: text, notes: r.notes?.trim() ?? null }));
          results.push({ success: true, topicId: r.topicId, subtopic: text });
          try { window.dispatchEvent(new CustomEvent('topics:updated', { detail: { topicId: r.topicId, subtopic: text, notes: r.notes?.trim() ?? null, subjectId: this.selectedSubjectId, schoolId } })); } catch {}
        } else {
          const res = await firstValueToPromise(this.api.createTopic(schoolId, this.selectedSubjectId, { subtopic: text, notes: r.notes?.trim() ?? null }));
          const createdId = (res as any)?.topicId ?? null;
          results.push({ success: true, topicId: createdId, subtopic: text });
          try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: createdId, subtopic: text, subjectId: this.selectedSubjectId, schoolId, notes: r.notes?.trim() ?? null } })); } catch {}
        }
      } catch (err) {
        results.push({ success: false, error: err, subtopic: text });
      }
    }

    // show a summary toast
    const successCount = results.filter(x => x.success).length;
    const failCount = results.length - successCount;
    const message = `${successCount} created/updated${failCount ? ' • ' + failCount + ' failed' : ''}`;
    const t = await this.toastCtrl.create({ message, duration: 2000, color: failCount ? 'warning' : 'success' });
    await t.present();
    // close modal and return results
    await this.popCtrl.dismiss({ results });
  }

  chooseExisting(topic: any): void {
    // add as a prefilled row for editing
    this.rows.push({ topicId: topic.topicId, subtopic: topic.subtopic ?? '', notes: topic.notes ?? null });
  }

  addRow(): void { this.rows.push({ subtopic: '', notes: '' }); }
  removeRow(i: number): void { if (i >= 0 && i < this.rows.length) this.rows.splice(i, 1); }

  dismiss(): void { void this.popCtrl.dismiss(); }
}

// tiny helper to convert observable to promise for async/await usage
function firstValueToPromise<T>(obs: import('rxjs').Observable<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    const sub = obs.subscribe({ next: v => { resolve(v); sub.unsubscribe(); }, error: e => { reject(e); sub.unsubscribe(); } });
  });
}
