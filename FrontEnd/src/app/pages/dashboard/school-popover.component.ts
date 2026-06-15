import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule, PopoverController, AlertController, ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ModalController } from '@ionic/angular';
import { ChatPerformanceComponent } from '../../components/chat-performance/chat-performance.component';
import { CreateTeacherPage } from '../create-teacher/create-teacher.page';
import { CreateStudentPage } from '../create-student/create-student.page';
import { AssignTeacherPage } from '../assign-teacher/assign-teacher.page';
import { CreateSubjectPage } from '../create-subject/create-subject.page';
import { CreateCoursePage } from '../create-course/create-course.page';
import { CreateLessonPage } from '../create-lesson/create-lesson.page';
import { CreateStagePage } from '../create-stage/create-stage.page';
import { CreateSubLevelPage } from '../create-sublevel/create-sublevel.page';
import { CreateTopicPage } from '../create-topic/create-topic.page';

@Component({
  selector: 'app-school-popover',
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule],
  template: `
    <div class="popover-root">
      <div class="popover-header">
        <div class="school-title">{{ schoolName || 'School' }}</div>
        <div class="header-actions">
          <ion-button class="settings-btn" *ngIf="isHeadTeacher" fill="clear" size="small" (click)="goToEdit()">⚙</ion-button>
        </div>
      </div>

      <div class="school-meta">ID: {{ schoolId || '—' }} • County: {{ county || '—' }}</div>

      <div class="actions-row">
        <div class="action-buttons">
          <ng-container *ngIf="isHeadTeacher">
              <ion-button size="small" expand="block" (click)="openAction('manage-teachers')">Manage teachers</ion-button>
            <ion-button size="small" expand="block" (click)="openAction('assign-teacher')">Assign teacher → subject</ion-button>
            <ion-button size="small" expand="block" (click)="openAction('create-subject')">Create subject</ion-button>
            <ion-button size="small" expand="block" color="danger" (click)="confirmClearTopics()">Clear all topics</ion-button>
              <!-- Create topic and Create sublevel removed per request -->
            <ion-button size="small" expand="block" (click)="openAction('manage-levels')">Manage levels</ion-button>
            <ion-button size="small" expand="block" (click)="openAction('manage-students')">Manage students</ion-button>
          </ng-container>
          <ng-container *ngIf="isTeacher && !isHeadTeacher">
            <ion-button size="small" expand="block" (click)="openAction('create-topic')">Create topic</ion-button>
          </ng-container>
        </div>

        <!-- Manage Teacher Topics removed -->
      </div>

      <!-- topics grid removed -->
    </div>
  `,
  styles: [
    `:host { --popover-max-width: 900px; }`,
    `.popover-root { padding: 14px; min-width: 0; max-width: var(--popover-max-width); width:100%; box-sizing:border-box; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial; overflow-x:hidden; }
     .popover-header { display:flex; align-items:center; justify-content:space-between; gap:8px; }
     .school-brand { display:flex; align-items:center; gap:10px; }
     .popover-logo { width:44px; height:44px; object-fit:cover; border-radius:8px; box-shadow:0 1px 4px rgba(0,0,0,0.12); }
     .popover-initial { width:44px; height:44px; display:flex; align-items:center; justify-content:center; background:var(--ion-color-primary); color:#fff; border-radius:8px; font-weight:700; font-size:18px; box-shadow:0 1px 4px rgba(0,0,0,0.08); }
    .school-title { font-weight:700; font-size:18px; }
    .header-actions { margin-left:auto; }
    .settings-btn { font-size:22px; line-height:1; --padding-start:6px; --padding-end:6px; }
     .school-meta { color: var(--ion-color-medium); font-size:13px; margin-top:6px; }
    .students-list { position:relative; }
    .students-list.header-origin { position: absolute; right: 14px; top:48px; z-index:60; }
    .students-inner { background:var(--ion-color-step-50); border:1px solid var(--ion-color-light-tint); padding:8px; border-radius:8px; box-shadow:0 6px 20px rgba(0,0,0,0.08); max-height:300px; overflow:auto; width:320px; }
    .students-title { font-weight:700; margin:0 0 8px 0; }
    .student-link { background:none; border:0; padding:6px 4px; text-align:left; width:100%; display:block; }
    .students-btn { font-weight:600; margin-right:6px; }
    .actions-row { display:flex; gap:12px; margin-top:12px; flex-wrap:wrap; }
    .action-buttons { display:flex; flex-direction:column; gap:8px; width:100%; max-width:240px; }
     .manage-section { flex:1; }
     .manage-title { font-weight:600; margin-bottom:6px; }
     .teacher-select ion-select { width:100%; }
     .topics-grid { margin-top:12px; }
     .multi-delete-bar { display:flex; justify-content:space-between; align-items:center; background: rgba(255,235,235,0.6); padding:8px; border-radius:8px; margin-bottom:8px; }
     .selected-info { font-weight:600; color:var(--ion-color-danger); }
     .delete-actions ion-button { margin-left:8px; }
     .subjects-column { max-height:420px; overflow:auto; padding-right:6px; display:grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap:10px; }
     .subject-card { border:1px solid var(--ion-color-light-tint); border-radius:10px; padding:10px; background: var(--ion-color-step-50); box-shadow: 0 2px 8px rgba(0,0,0,0.03); }
     .subject-header { font-weight:700; margin-bottom:6px; font-size:15px; }
     .topics-list { --ion-item-background: transparent; }
     .topic-item { --padding-start: 8px; --padding-end: 8px; align-items:flex-start; }
     .topic-label { cursor:pointer; flex:1; }
     .topic-title { margin:0; font-size:14px; }
     .topic-notes { margin:4px 0 0; color:var(--ion-color-medium); font-size:13px; }
     .topic-actions { display:flex; gap:6px; align-items:center; }
     .subtopics { margin-top:8px; padding-left:12px; }
     .no-data, .no-topics, .no-subtopics { color:var(--ion-color-medium); font-size:13px; }
     @media (max-width: 520px) { .popover-root { min-width: 0; padding:10px; } .action-buttons { width:100%; max-width:100%; } .subjects-column { grid-template-columns: 1fr; } }
    `
  ],
})
export class SchoolPopoverComponent implements OnInit, OnDestroy {
  @Input() schoolId?: string | null;
  @Input() schoolName?: string | null;
  @Input() county?: string | null;

  isTeacher = false;
  isHeadTeacher = false;

  // popover only shows school actions now; learners are managed via dashboard header

  constructor(private router: Router, private popCtrl: PopoverController, private api: ApiService, private alertCtrl: AlertController, private toastCtrl: ToastController, private modalCtrl: ModalController) {
    const auth = this.api.loadAuth();
    const roles = (auth?.roles ?? []) as string[];
    this.isHeadTeacher = roles.some(r => r && r.toLowerCase() === 'headteacher');
    this.isTeacher = roles.some(r => r && r.toLowerCase() === 'teacher');
  }

  async confirmClearTopics(): Promise<void> {
    if (!this.schoolId) return;
    const alert = await this.alertCtrl.create({
      header: 'Confirm',
      message: 'Soft-delete all topics and subtopics for this school? This cannot be undone via UI.',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Clear', role: 'destructive', handler: () => void this.clearTopics() }
      ]
    });
    await alert.present();
  }

  private async clearTopics(): Promise<void> {
    if (!this.schoolId) return;
    try {
      const r = await this.api.clearTopicsForSchool(this.schoolId).toPromise();
      // inform other components to refresh
      try { window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { scope: 'cleared', count: r?.count ?? 0 } })); } catch {}
      try { window.dispatchEvent(new CustomEvent('topics:cleared', { detail: { count: r?.count ?? 0 } })); } catch {}
      const t = await this.toastCtrl.create({ message: `Cleared ${r?.count ?? 0} topic(s)`, duration: 1800, color: 'success' });
      await t.present();
    } catch (err) {
      console.error('Failed to clear topics', err);
      const t = await this.toastCtrl.create({ message: 'Failed to clear topics', duration: 2000, color: 'danger' });
      await t.present();
    }
  }

  ngOnInit(): void {
    try { window.addEventListener('topics:deleted', this.externalTopicDeleted as EventListener); } catch {}
  }

  ngOnDestroy(): void {
    try { window.removeEventListener('topics:deleted', this.externalTopicDeleted as EventListener); } catch {}
  }

  // minimal handler kept to avoid errors when other components dispatch this event
  private externalTopicDeleted = (_ev: Event) => { /* no-op in this popover */ };

  goToEdit(): void {
    this.router.navigateByUrl('/school/edit');
    void this.popCtrl.dismiss();
  }

  async openAction(action: string): Promise<void> {
    try { await this.popCtrl.dismiss(); } catch {}

    let comp: any = null;
    switch (action) {
      case 'create-student': comp = CreateStudentPage; break;
      case 'create-teacher': comp = CreateTeacherPage; break;
      case 'manage-teachers':
        this.router.navigateByUrl('/manage/teachers');
        return;
      case 'assign-teacher': comp = AssignTeacherPage; break;
      case 'create-subject': comp = CreateSubjectPage; break;
      case 'create-course': comp = CreateCoursePage; break;
      case 'create-lesson': comp = CreateLessonPage; break;
      case 'create-stage': comp = CreateStagePage; break;
      // create-sublevel removed
      case 'manage-students':
        // navigate to manage students page
        this.router.navigateByUrl('/manage/students');
        return;
      case 'manage-levels':
        this.router.navigateByUrl('/manage/levels');
        return;
      default: return;
    }

    const p = await this.popCtrl.create({ component: comp, translucent: true });
    await p.present();
    const r = await p.onWillDismiss();
    if (action === 'create-stage' && r && r.data) {
      try { window.dispatchEvent(new CustomEvent('stages:created', { detail: r.data })); } catch { }
    }
  }

  toggleStudents(ev?: Event): void {
    // removed - learners list moved to dashboard header
  }

  openLearnerPerformance(student: { userId?: string; username?: string; firstName?: string | null; lastName?: string | null }) {
    if (!student?.userId) return;
    // navigate to learner performance page
    try { void this.popCtrl.dismiss(); } catch {}
    this.router.navigateByUrl(`/learner/${student.userId}`);
  }

  async openSchoolPopover(ev?: Event): Promise<void> {
    try {
      const schoolId = this.schoolId ?? (this.api.loadAuth() as any)?.schoolId ?? null;
      const modal = await this.modalCtrl.create({ component: ChatPerformanceComponent, componentProps: { schoolId }, cssClass: 'performance-chat-modal' });
      await modal.present();
    } catch (err) {
      console.warn('Failed to open performance chat', err);
    }
  }

  studentDisplayName(s: { firstName?: string | null; lastName?: string | null; username?: string | null }): string {
    const parts: string[] = [];
    if (s.firstName) parts.push(s.firstName);
    if (s.lastName) parts.push(s.lastName);
    if (parts.length) return parts.join(' ');
    return s.username ?? 'Learner';
  }
}
