import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ModalController } from '@ionic/angular';
import { TopicPerformanceComponent } from '../../components/topic-performance/topic-performance.component';
import { StudentLearningComponent } from '../../components/student-learning/student-learning.component';

@Component({
  selector: 'app-learner-performance',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './learner-performance.page.html',
  styleUrls: ['./learner-performance.page.scss']
})
export class LearnerPerformancePage implements OnInit, OnDestroy {
  userId?: string | null;
  schoolId?: string | null;
  studentName?: string;
  subjects: Array<{ subjectId: string; name: string; stage: number }> = [];
  performance: Array<{ subjectId: string; name: string; score: number; topicCount?: number | null }> = [];
  average = 0;
  stages: Array<{ stageId?: string; name: string; label?: string | null; description?: string | null; }> = [];

  // enrich performance with level label
  enrichedPerformance: Array<{ subjectId: string; name: string; score: number; level: string; topicCount?: number | null }> = [];
  overallLevel: string = '—';

  constructor(private route: ActivatedRoute, private router: Router, private api: ApiService, private modalCtrl: ModalController) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('userId');
    const auth = this.api.loadAuth?.() ?? null;
    this.schoolId = auth?.schoolId ?? null;
    this.studentName = this.userId ? (`Learner ${this.userId?.slice(0,6)}`) : 'Learner';

    // attempt to load the real student name from backend if available
    if (this.schoolId && this.userId) {
      this.api.getStudents(this.schoolId).subscribe({ next: st => {
        const found = (st ?? []).find((s: any) => s.userId === this.userId);
        if (found) this.studentName = (found.firstName || found.lastName) ? `${found.firstName ?? ''} ${found.lastName ?? ''}`.trim() || found.username : found.username;
      }, error: () => {} });
    }

    // Load subjects depending on viewer role:
    // - Head teacher sees all subjects for the school
    // - Teacher sees only their assigned subjects
    // - Others fall back to all subjects
    const roles = Array.isArray(auth?.roles) ? (auth!.roles as string[]) : [];
    const isHead = roles.some(r => String(r).toLowerCase() === 'headteacher');
    const isTeacher = roles.some(r => String(r).toLowerCase() === 'teacher');

    if (!this.schoolId) {
      this.onSubjects([]);
      return;
    }

    // Choose which subjects to load depending on role
    if (isHead) {
      // Head teacher: load full subject list for the school
      this.api.getSubjects(this.schoolId).subscribe({ next: subs => this.onSubjects(subs ?? []), error: () => this.onSubjects([]) });
    } else if (isTeacher && auth?.userId) {
      // Teacher: prefer assigned subjects, but fallback to all subjects if none assigned
      this.api.getTeacherSubjects(this.schoolId, auth.userId).subscribe({ next: subs => {
        if (subs && subs.length) this.onSubjects(subs); else this.api.getSubjects(this.schoolId!).subscribe({ next: s => this.onSubjects(s ?? []), error: () => this.onSubjects([]) });
      }, error: () => { this.api.getSubjects(this.schoolId!).subscribe({ next: s => this.onSubjects(s ?? []), error: () => this.onSubjects([]) }); } });
    } else {
      // default: load all subjects
      this.api.getSubjects(this.schoolId).subscribe({ next: subs => this.onSubjects(subs ?? []), error: () => this.onSubjects([]) });
    }

    // Listen for topic lifecycle events so learner performance updates when topics change
    try {
      window.addEventListener('topics:deleted', this.topicsDeletedListener);
      window.addEventListener('topics:created', this.topicsCreatedListener);
    } catch {}
    
  }


  ngOnDestroy(): void {
    try {
      window.removeEventListener('topics:deleted', this.topicsDeletedListener);
      window.removeEventListener('topics:created', this.topicsCreatedListener);
    } catch {}
  }

  private topicsCreatedListener = (ev: any) => {
    try {
      const d = ev?.detail;
      if (!d) return;
      const subjectId = d.subjectId ?? null;
      if (!subjectId) return;
      // update counts for the subject that changed
      this.updateSubjectTopicCount(subjectId);
    } catch (err) { /* ignore */ }
  };

  private topicsDeletedListener = (ev: any) => {
    try {
      const d = ev?.detail;
      if (!d) return;
      // If a wholesale clear occurred, refresh everything
      if (d.scope === 'cleared') {
        for (const s of this.subjects) {
          this.updateSubjectTopicCount(s.subjectId);
        }
        return;
      }
      const subjectId = d.subjectId ?? null;
      if (!subjectId) return;
      this.updateSubjectTopicCount(subjectId);
    } catch (err) { /* ignore */ }
  };

  private updateSubjectTopicCount(subjectId: string): void {
    if (!this.schoolId || !subjectId) return;
    // find the performance entry for subject and refresh the topic count
    const idx = this.performance.findIndex(p => p.subjectId === subjectId);
    if (idx < 0) return;
    this.api.getTopics(this.schoolId, subjectId).subscribe({ next: (topics: any) => {
      const count = (topics || []).length;
      const derived = Math.min(95, 45 + count * 8);
      this.performance[idx].score = Math.round(derived);
      (this.performance[idx] as any).topicCount = count;
      this.enrichPerformance();
    }, error: () => { (this.performance[idx] as any).topicCount = 0; this.enrichPerformance(); } });
  }

  private onSubjects(subs: Array<any>) {
    // Use backend subjects if present, otherwise synthetic fallback
    this.subjects = (subs && subs.length) ? subs.map(s => ({ subjectId: s.subjectId, name: s.name, stage: s.stage })) :
      [ { subjectId: 's-math', name: 'Mathematics', stage: 1 }, { subjectId: 's-eng', name: 'English', stage: 1 }, { subjectId: 's-science', name: 'Science', stage: 1 } ];

    // For each subject, load real Topics from backend and synthesize a subject-level score
    this.performance = [];
    for (const s of this.subjects) {
      // default values while loading
      this.performance.push({ subjectId: s.subjectId, name: s.name, score: 0 });
      if (this.schoolId) {
        this.api.getTopics(this.schoolId, s.subjectId).subscribe({ next: (topics: any) => {
          // Use number of topics to derive an indicative score: more topics -> higher coverage
          const count = (topics || []).length;
          const derived = Math.min(95, 45 + count * 8); // simple heuristic
          const idx = this.performance.findIndex(p => p.subjectId === s.subjectId);
          if (idx >= 0) {
            this.performance[idx].score = Math.round(derived);
            (this.performance[idx] as any).topicCount = count;
            this.enrichPerformance();
          }
        }, error: () => {
          const idx = this.performance.findIndex(p => p.subjectId === s.subjectId);
          if (idx >= 0) { this.performance[idx].score = 45; (this.performance[idx] as any).topicCount = 0; this.enrichPerformance(); }
        } });
      } else {
        const idx = this.performance.findIndex(p => p.subjectId === s.subjectId);
        if (idx >= 0) { this.performance[idx].score = Math.round(45 + Math.random() * 50); this.enrichPerformance(); }
      }
    }

    // try fetching stage definitions from backend to map stage -> label (head teacher created stages)
    if (this.schoolId) {
      this.api.getStages(this.schoolId).subscribe({ next: st => { this.stages = st ?? []; this.enrichPerformance(); }, error: () => { this.enrichPerformance(); } });
    } else {
      this.enrichPerformance();
    }
  }

  private enrichPerformance() {
    // compute level label per subject either from subject.stage -> stages mapping, or derive from score
    this.enrichedPerformance = this.performance.map(p => {
      const subj = this.subjects.find(s => s.subjectId === p.subjectId);
      const stageNum = subj?.stage ?? null;
      let level = this.levelFromScore(p.score);
      if (stageNum !== null && this.stages && this.stages.length) {
        // if stage names were created by head teacher, attempt to find stage by index/name
        const sdef = this.stages.find(s => (s.label == String(stageNum) || s.name == String(stageNum) || (s as any).stageId == String(stageNum)));
        if (sdef) level = sdef.name || level;
      }
      return { ...p, level, topicCount: (p as any).topicCount ?? null };
    });
    this.average = Math.round(this.enrichedPerformance.reduce((a, b) => a + b.score, 0) / (this.enrichedPerformance.length || 1));

    // Determine overall level (most common level among subjects)
    if (this.enrichedPerformance.length) {
      const counts: Record<string, number> = {};
      for (const p of this.enrichedPerformance) counts[p.level] = (counts[p.level] ?? 0) + 1;
      let max = 0; let chosen = '';
      for (const k of Object.keys(counts)) {
        if (counts[k] > max) { max = counts[k]; chosen = k; }
      }
      this.overallLevel = chosen || this.enrichedPerformance[0].level;
    } else {
      this.overallLevel = '—';
    }
  }

  private levelFromScore(score: number): string {
    if (score < 50) return 'Beginner';
    if (score < 75) return 'Intermediate';
    return 'Advanced';
  }

  back() { this.router.navigateByUrl('/teacher'); }

  async openSubjectTopics(subjectId?: string, subjectName?: string) {
    if (!subjectId || !this.schoolId) return;
    try {
      const m = await this.modalCtrl.create({
        component: TopicPerformanceComponent,
        componentProps: { schoolId: this.schoolId, subjectId, subjectName, userId: this.userId },
        cssClass: 'topic-performance-modal'
      });
      await m.present();
    } catch (e) {
      console.warn('Failed to open topic performance modal', e);
    }
  }

  async openStudy(subjectId?: string, subjectName?: string) {
    if (!subjectId || !this.schoolId) return;
    try {
      const m = await this.modalCtrl.create({
        component: StudentLearningComponent,
        componentProps: { schoolId: this.schoolId, subjectId, subjectName },
        cssClass: 'student-learning-modal'
      });
      await m.present();
    } catch (e) {
      console.warn('Failed to open study modal', e);
    }
  }
}
