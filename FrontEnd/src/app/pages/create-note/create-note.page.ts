import { Component, Input, OnInit, ViewChild } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-create-note',
    imports: [FormsModule, IonicModule],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Notes</ion-title>
        <ion-buttons slot="end">
          <ion-button fill="clear" (click)="dismiss()">✕</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="notes-content">
      @if (!topicId) {
        <div class="no-topic">No topic selected.</div>
      }
      @if (topicId) {
        <div class="notes-container">
          <label class="notes-label">Notes</label>
          <textarea class="large-notes" [(ngModel)]="notes" placeholder="Write notes here..."></textarea>
        </div>
      }
    
      <div class="notes-actions">
        <ion-button expand="block" (click)="save()" [disabled]="!canSave">Save</ion-button>
        <ion-button expand="block" fill="clear" (click)="dismiss()">Cancel</ion-button>
      </div>
    </ion-content>
    `,
    styles: [
        `
    .notes-content {
      display: flex;
      flex-direction: column;
      height: 100%;
      padding: 12px;
      box-sizing: border-box;
    }
    .no-topic { color: var(--ion-color-medium); padding: 12px; }
    .notes-container { display: flex; flex-direction: column; flex: 1 1 auto; min-height: 200px; }
    .notes-label { font-weight: 700; margin-bottom: 8px; color: var(--ion-color-dark); }
    .large-notes {
      flex: 1 1 auto;
      width: 100%;
      min-height: 260px;
      max-height: calc(100vh - 220px);
      padding: 12px;
      box-sizing: border-box;
      border-radius: 8px;
      border: 1px solid rgba(0,0,0,0.08);
      resize: both;
      overflow: auto;
      font-size: 15px;
      line-height: 1.4;
      background: var(--ion-background-color, #fff);
    }
    /* Push actions to the bottom so notes area has maximum room */
    .notes-actions { display:flex; gap:8px; margin-top:12px; margin-top: auto; padding-top:8px; }
      .drawing-area { flex: 1 1 auto; display: flex; min-height: 260px; margin-top: 8px; }
    @media (max-width: 600px) {
      .large-notes { min-height: 200px; }
    }
    `
    ]
})
export class CreateNotePage implements OnInit {
  @Input() topicId?: string | null;
  @Input() subjectId?: string | null;
  @Input() schoolId?: string | null;
  @Input() subTopicId?: string | null;
  notes: string = '';
  

  constructor(private api: ApiService, private modalCtrl: ModalController, private toastCtrl: ToastController) {}

  ngOnInit(): void {
    // Load existing notes for the passed topicId (if any) using the new single-topic endpoint.
    try {
      // If a subTopicId was provided, prefer loading notes from the SubTopic record.
      if (this.subTopicId && this.topicId) {
        // Load parent topic and find the matching subtopic in the returned list
        this.api.getTopic(this.topicId).subscribe({
          next: t => {
            try {
              const found = (t?.subtopics ?? []).find((st: any) => String(st.subTopicId) === String(this.subTopicId));
              if (found) {
                this.notes = found.notes ?? '';
              } else {
                // fallback to topic-level notes if subtopic not found
                this.notes = t?.notes ?? '';
              }
            } catch (e) {
              this.notes = t?.notes ?? '';
            }
            console.debug('CreateNotePage: loaded notes for subTopic/topic', this.subTopicId ?? this.topicId);
          },
          error: (err) => { console.debug('CreateNotePage: getTopic failed', err); }
        });
        return;
      }

      if (!this.topicId) return;
      this.api.getTopic(this.topicId).subscribe({
        next: t => {
          this.notes = t?.notes ?? '';
          console.debug('CreateNotePage: loaded topic via getTopic', this.topicId);
        },
        error: (err) => { console.debug('CreateNotePage: getTopic failed', err); }
      });
    } catch (ex) {
      console.debug('CreateNotePage: failed to load notes', ex);
    }
  }

  get canSave(): boolean { return !!(this.topicId || this.subTopicId); }

  async save(): Promise<void> {
    if ((!this.topicId && !this.subTopicId) || !this.subjectId || !this.schoolId) return;
    try {
      const payload: any = { Notes: this.notes };
      if (this.subTopicId && this.topicId) {
        // Save notes to SubTopic
        await firstValueToPromise(this.api.updateSubTopic(this.schoolId, this.subjectId, this.topicId, this.subTopicId, payload));
      } else if (this.topicId) {
        // Fallback: save notes to Topic
        await firstValueToPromise(this.api.updateTopic(this.schoolId, this.subjectId, this.topicId, { Notes: this.notes }));
      }
      try { window.dispatchEvent(new CustomEvent('topics:updated', { detail: { topicId: this.topicId, notes: this.notes, subjectId: this.subjectId, schoolId: this.schoolId } })); } catch {}
      const t = await this.toastCtrl.create({ message: 'Notes saved', duration: 1500, color: 'success' });
      await t.present();
      await this.modalCtrl.dismiss({ topicId: this.topicId, subTopicId: this.subTopicId });
    } catch (err: any) {
      const t = await this.toastCtrl.create({ message: err?.message ?? 'Failed to save notes', duration: 2500, color: 'danger' });
      await t.present();
    }
  }

  dismiss(): void { void this.modalCtrl.dismiss(); }
}

function firstValueToPromise<T>(obs: import('rxjs').Observable<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    const sub = obs.subscribe({ next: v => { resolve(v); sub.unsubscribe(); }, error: e => { reject(e); sub.unsubscribe(); } });
  });
}
