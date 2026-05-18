import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';
import { CreateNotePage } from '../../pages/create-note/create-note.page';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-topic-performance',
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule],
  templateUrl: './topic-performance.component.html',
  styleUrls: ['./topic-performance.component.scss']
})
export class TopicPerformanceComponent implements OnInit {
  @Input() schoolId?: string | null;
  @Input() subjectId?: string | null;
  @Input() subjectName?: string | null;
  @Input() topicId?: string | null; // optional: if provided, focus the view on a single topic
  @Input() userId?: string | null;

  topics: Array<{ topicId: string; subtopic: string; notes?: string | null; score?: number; name?: string | null; parentTopicId?: string | null; _subjectId?: string | null }> = [];
  // raw topics returned from backend (includes both parents and children)
  allTopics: Array<{ topicId: string; subtopic: string; notes?: string | null; name?: string | null; parentTopicId?: string | null; _subjectId?: string | null } | any> = [];
  average = 0;
  noTopics: boolean = false;
  // store fetched topic details keyed by topicId
  topicDetails: Record<string, any> = {};
  expandedTopic?: string | null = null;
  // editing state
  editingTopicId?: string | null = null;
  editModels: Record<string, { name?: string | null; notes?: string | null }> = {};

  constructor(private modalCtrl: ModalController, private api: ApiService, private toastCtrl: ToastController) {}

  ngOnInit(): void {
    if (this.schoolId && this.subjectId) {
      this.api.getTopics(this.schoolId, this.subjectId).subscribe({ next: t => this.onTopics(t ?? []), error: async (err: any) => { console.warn('Failed to load topics', err); const msg = err?.message ? `${err.message}` : 'Failed to load topics'; const st = (err && (err as any).status) ? ` (${(err as any).status})` : ''; const t = await this.toastCtrl.create({ message: msg + st, duration: 2500, color: 'warning' }); await t.present(); this.onTopics([]); } });
    } else if (this.schoolId && !this.subjectId) {
      // No subject specified: aggregate topics for all subjects in the school (useful for learner-level views)
      (async () => {
        try {
          const subjects = await firstValueFrom(this.api.getSubjects(this.schoolId!));
          const arr: Array<any> = [];
          if (subjects && subjects.length) {
            // fetch topics for each subject in parallel
            const promises = (subjects || []).map(s => firstValueFrom(this.api.getTopics(this.schoolId!, s.subjectId))
              .then(r => ({ subjectId: s.subjectId, topics: r ?? [] }))
              .catch(() => ({ subjectId: s.subjectId, topics: [] })));
            const results = await Promise.all(promises);
            for (const r of results) {
              if (r && Array.isArray(r.topics)) {
                // attach subjectId to each topic item so consumers can filter
                for (const t of r.topics) { (t as any)._subjectId = r.subjectId; arr.push(t); }
              }
            }
          }
          this.onTopics(arr);
        } catch (err) {
          console.warn('Failed to aggregate topics for school', err);
          this.onTopics([]);
        }
      })();
    } else {
      this.onTopics([]);
    }
    // Listen for topic lifecycle events so performance UI stays in sync
    try {
      window.addEventListener('topics:created', this.topicsCreatedListener);
      window.addEventListener('topics:deleted', this.topicsDeletedListener);
    } catch {}
  }

  ngOnDestroy(): void {
    try {
      window.removeEventListener('topics:created', this.topicsCreatedListener);
      window.removeEventListener('topics:deleted', this.topicsDeletedListener);
    } catch {}
  }

  private topicsCreatedListener = (ev: any) => {
    try {
      const d = ev?.detail;
      if (!d) return;
      const subjectId = d.subjectId ?? null;
      // If a specific subjectId is configured, only refresh for that subject; otherwise (aggregated view) refresh for any created topic in the school
      const createdSchoolId = d.schoolId ?? null;
      if (this.subjectId) {
        if (subjectId && String(subjectId) === String(this.subjectId)) {
          if (this.schoolId && this.subjectId) this.api.getTopics(this.schoolId, this.subjectId).subscribe({ next: t => this.onTopics(t ?? []), error: () => {} });
        }
      } else {
        // aggregated across school: refresh when the created topic belongs to this school (or if schoolId not provided, refresh anyway)
        if (!createdSchoolId || (this.schoolId && String(createdSchoolId) === String(this.schoolId))) {
          if (this.schoolId) {
            // re-aggregate topics across subjects
            (async () => {
              try {
                const subjects = await firstValueFrom(this.api.getSubjects(this.schoolId!));
                const arr: Array<any> = [];
                if (subjects && subjects.length) {
                  const promises = (subjects || []).map(s => firstValueFrom(this.api.getTopics(this.schoolId!, s.subjectId)).then(r => ({ subjectId: s.subjectId, topics: r ?? [] })).catch(() => ({ subjectId: s.subjectId, topics: [] })));
                  const results = await Promise.all(promises);
                  for (const r of results) {
                    if (r && Array.isArray(r.topics)) for (const t of r.topics) { (t as any)._subjectId = r.subjectId; arr.push(t); }
                  }
                }
                this.onTopics(arr);
              } catch (err) { /* ignore refresh errors */ }
            })();
          }
        }
      }
    } catch (err) { /* ignore */ }
  };

  private topicsDeletedListener = (ev: any) => {
    try {
      const d = ev?.detail;
      if (!d) return;
      // If a wholesale clear occurred, reload and derive an empty state
      if (d.scope === 'cleared') {
        if (this.schoolId && this.subjectId) this.api.getTopics(this.schoolId, this.subjectId).subscribe({ next: t => this.onTopics(t ?? []), error: () => {} });
        return;
      }
      const subjectId = d.subjectId ?? null;
      // If a specific subjectId is configured, refresh only for that subject; otherwise (aggregated view) refresh for any deletion in this school
      const deletedSchoolId = d.schoolId ?? null;
      if (this.subjectId) {
        if (!subjectId || String(subjectId) === String(this.subjectId)) {
          if (this.schoolId && this.subjectId) this.api.getTopics(this.schoolId, this.subjectId).subscribe({ next: t => this.onTopics(t ?? []), error: () => {} });
        }
      } else {
        if (!deletedSchoolId || (this.schoolId && String(deletedSchoolId) === String(this.schoolId))) {
          // re-aggregate topics across subjects
          (async () => {
            try {
              const subjects = await firstValueFrom(this.api.getSubjects(this.schoolId!));
              const arr: Array<any> = [];
              if (subjects && subjects.length) {
                const promises = (subjects || []).map(s => firstValueFrom(this.api.getTopics(this.schoolId!, s.subjectId)).then(r => ({ subjectId: s.subjectId, topics: r ?? [] })).catch(() => ({ subjectId: s.subjectId, topics: [] })));
                const results = await Promise.all(promises);
                for (const r of results) {
                  if (r && Array.isArray(r.topics)) for (const t of r.topics) { (t as any)._subjectId = r.subjectId; arr.push(t); }
                }
              }
              this.onTopics(arr);
            } catch (e) { /* ignore */ }
          })();
        }
      }
    } catch (err) { /* ignore */ }
  };

  private onTopics(t: Array<any>) {
    // keep raw list for deriving subtopics later
    this.allTopics = t || [];
    // Show only top-level (parent) topics in the main list; subtopics are shown when a parent is expanded
    const parents = (this.allTopics || []).filter(x => !(x.parentTopicId));
    if (parents && parents.length) {
      this.topics = parents.map((x: any, i: number) => ({ topicId: x.topicId ?? `t${i+1}`, subtopic: x.subtopic ?? ('Topic ' + (i+1)), name: x.name ?? x.TopicName ?? x.topicName ?? null, notes: x.notes ?? null, parentTopicId: x.parentTopicId ?? null, score: Math.round(40 + Math.random() * 55) }));
      this.noTopics = false;
    } else if (this.allTopics && this.allTopics.length) {
      // Fallback: if there are no parent topics but we have topics (all are subtopics), show them as top-level
      this.topics = (this.allTopics || []).map((x: any, i: number) => ({ topicId: x.topicId ?? `t${i+1}`, subtopic: x.subtopic ?? ('Topic ' + (i+1)), name: x.name ?? x.TopicName ?? x.topicName ?? null, notes: x.notes ?? null, parentTopicId: x.parentTopicId ?? null, score: Math.round(40 + Math.random() * 55) }));
      this.noTopics = false;
    } else {
      this.topics = [];
      this.noTopics = true;
    }
    // If a specific topicId was provided, filter to that topic only
    if (this.topicId) {
      // If a specific topicId (possibly a subtopic) is requested, try to focus its parent
      const target = (this.allTopics || []).find(x => String(x.topicId) === String(this.topicId));
      if (target) {
        const parentId = target.parentTopicId ?? null;
        // Prefer to focus parent; if parent is missing, focus the target itself
        const parent = parentId ? (this.allTopics || []).find(x => String(x.topicId) === String(parentId)) : null;
        if (parent) {
          this.topics = [parent];
          this.expandedTopic = parent.topicId;
          const derived = (this.allTopics || []).filter((tt: any) => (tt.parentTopicId ?? null) === parent.topicId);
          if (derived && derived.length) {
            this.topicDetails[parent.topicId] = this.topicDetails[parent.topicId] ?? {};
            this.topicDetails[parent.topicId].subtopics = derived.map((d: any) => ({ topicId: d.topicId, subtopic: d.subtopic ?? d.name ?? d.title ?? d, name: d.name ?? null, notes: d.notes ?? null, score: 75 }));
          }
        } else {
          // Parent not present (data inconsistency): show the target as the only topic and expand it
          this.topics = [target];
          this.expandedTopic = target.topicId;
          this.topicDetails[target.topicId] = this.topicDetails[target.topicId] ?? {};
          // find siblings/children if any (children of target)
          const derived = (this.allTopics || []).filter((tt: any) => (tt.parentTopicId ?? null) === target.topicId);
          if (derived && derived.length) this.topicDetails[target.topicId].subtopics = derived.map((d: any) => ({ topicId: d.topicId, subtopic: d.subtopic ?? d.name ?? d.title ?? d, name: d.name ?? null, notes: d.notes ?? null, score: 75 }));
        }
      }
    }

    this.average = Math.round(this.topics.reduce((a, b) => a + (b.score ?? 0), 0) / (this.topics.length || 1));
  }

  async toggleTopic(topicId: string) {
    if (!topicId) return;
    if (this.expandedTopic === topicId) { this.expandedTopic = null; return; }
    this.expandedTopic = topicId;
    // load detailed topic info from backend (real DB)
    try {
      const details = await firstValueFrom(this.api.getTopic(topicId));
      this.topicDetails[topicId] = details ?? {};
      // If backend doesn't include a `subtopics` array, try to derive subtopics
      // by fetching all topics for this subject and filtering by parent id fields.
      if (!this.topicDetails[topicId].subtopics?.length && this.allTopics && this.allTopics.length) {
        const derived = (this.allTopics || []).filter((tt: any) => {
          const pid = (tt as any).parentTopicId ?? (tt as any).parentId ?? (tt as any).parentTopic ?? (tt as any).parent ?? null;
          return pid === topicId;
        });
        if (derived && derived.length) {
          this.topicDetails[topicId].subtopics = derived.map((d: any) => ({ topicId: d.topicId, subtopic: d.subtopic ?? d.name ?? d.title ?? d, name: d.name ?? d.TopicName ?? d.topicName ?? null, notes: d.notes ?? null, score: 75 }));
        }
      }
    } catch (e) {
      // if backend doesn't provide extra details, fall back to existing mapped topic
      this.topicDetails[topicId] = this.topics.find(t => t.topicId === topicId) || {};
    }
  }

  async openSubtopicNotes(sub: any) {
    const parentTopicId = sub?.topicId ?? this.expandedTopic ?? null;
    if (!parentTopicId) return;
    try {
      // If the sub item represents a SubTopic (has subTopicId), pass that so modal saves to SubTopic
      const props: any = { topicId: parentTopicId, subjectId: this.subjectId, schoolId: this.schoolId };
      if (sub?.subTopicId) props.subTopicId = sub.subTopicId;

      const m = await this.modalCtrl.create({ component: CreateNotePage, componentProps: props, cssClass: 'full-screen-modal' });
      await m.present();
      const res = await m.onDidDismiss();
      // After notes saved, refresh the subtopic/topic notes from API
      try {
        const updated = await firstValueFrom(this.api.getTopic(parentTopicId));
        if (updated) {
          if (sub?.subTopicId) {
            const found = (updated.subtopics ?? []).find((st: any) => String(st.subTopicId) === String(sub.subTopicId));
            if (found) sub.notes = found.notes ?? sub.notes;
          } else {
            sub.notes = updated.notes ?? sub.notes;
          }
        }
      } catch { /** ignore refresh errors */ }
    } catch (err) {
      console.error('Failed to open notes for subtopic', err);
    }
  }

  startEdit(topic: any, event?: Event) {
    if (event) event.stopPropagation();
    const id = topic.topicId ?? topic.topicId;
    this.editingTopicId = id;
    const existing = this.topicDetails[id] ?? this.topics.find(t => t.topicId === id) ?? {};
    this.editModels[id] = { name: existing.name ?? existing.TopicName ?? null, notes: existing.notes ?? null };
    // ensure it's expanded so the edit form is visible
    this.expandedTopic = id;
  }

  cancelEdit(topicId?: string) {
    if (!topicId) { this.editingTopicId = null; return; }
    delete this.editModels[topicId];
    this.editingTopicId = null;
  }

  async saveEdit(topicId: string) {
    if (!topicId) return;
    const model = this.editModels[topicId];
    if (!model) return;
    if (!this.schoolId || !this.subjectId) return;
    try {
      await firstValueFrom(this.api.updateTopic(this.schoolId, this.subjectId, topicId, { Subtopic: undefined, Notes: model.notes ?? null, TopicName: model.name ?? null, ParentTopicId: undefined }));
      // update local copies
      const t = this.topics.find(x => x.topicId === topicId);
      if (t) {
        t.name = model.name ?? t.name;
        t.notes = model.notes ?? t.notes;
      }
      if (!this.topicDetails[topicId]) this.topicDetails[topicId] = {};
      this.topicDetails[topicId].name = model.name ?? this.topicDetails[topicId].name;
      this.topicDetails[topicId].notes = model.notes ?? this.topicDetails[topicId].notes;
      this.editingTopicId = null;
      delete this.editModels[topicId];
    } catch (e) {
      console.error('Failed to save topic', e);
      // Optionally surface an error to the user
    }
  }

  close() { this.modalCtrl.dismiss(); }
}
