import { Component, OnInit, ViewChildren, QueryList, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';
import { ToastController, PopoverController, ModalController, AlertController } from '@ionic/angular';
import { Router } from '@angular/router';
import { ChatPerformanceComponent } from '../../components/chat-performance/chat-performance.component';
import { StudentListModalComponent } from '../../components/student-list-modal/student-list-modal.component';
import { CreateNotePage } from '../create-note/create-note.page';
import { addIcons } from 'ionicons';
import { flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline, logOutOutline, closeOutline, bookOutline } from 'ionicons/icons';
// using native inputs for inline pill editing (ElementRef targets)

interface PromptRoom {
  id: string;
  title: string;
  category: string;
  timeLeft: string;
  streakDays: number;
  participants: number;
  mood: 'violet' | 'teal' | 'amber' | 'pink';
  icon?: string;
}

interface StoryCard {
  id: string;
  promptId: string;
  subjectId?: string | null;
  user: string;
  timeAgo: string;
  reactions: number;
  remixes: number;
  badge?: string;
  // editable labels returned from modal or edited inline
  reactionsLabel?: string;
  remixesLabel?: string;
  reactLabel?: string;
  remixLabel?: string;
  // editing flags for inline edits on the story card
  editingReactions?: boolean;
  editingRemixes?: boolean;
  editingReact?: boolean;
  editingRemix?: boolean;
  // editable name and subtopics
  editingName?: boolean;
  reactionsSubtopic?: string | null;
  remixesSubtopic?: string | null;
  reactSubtopic?: string | null;
  remixSubtopic?: string | null;
  // additional dynamic subtopics when Add is clicked (store objects to keep topicId)
  extraSubtopics?: Array<{ topicId?: string; subtopic: string; notes?: string | null; createdAt?: string | null; updatedAt?: string | null; createdByUserId?: string | null; updatedByUserId?: string | null }>;
  // topicId for the story-level topic (created via Save on the card title)
  topicId?: string;
  // index of an extra subtopic currently being edited (shows input)
  editingExtraIndex?: number | null;
  // index of extra subtopic currently collapsing (keeps editor in DOM while animating)
  editorCollapsingIndex?: number | null;
  // internal marker used by frontend to note when a story was created locally (ms since epoch)
  _localCreatedAt?: number;
}
 

@Component({
    selector: 'app-teacher',
    imports: [CommonModule, FormsModule, IonicModule, RouterModule],
    templateUrl: './teacher.page.html',
    styleUrls: ['./teacher.page.scss']
})
export class TeacherPage implements OnInit, OnDestroy {
  private readonly STORIES_KEY = 'nile.stories';
  prompts: PromptRoom[] = [
    { id: 'p1', title: 'Create Topics', category: 'Momentum', timeLeft: '12h left', streakDays: 4, participants: 182, mood: 'violet', icon: 'book-outline' },
    { id: 'p2', title: 'Weekend plans in 15s', category: 'Social', timeLeft: '8h left', streakDays: 0, participants: 96, mood: 'teal' },
    { id: 'p3', title: 'Desk setup snapshot', category: 'Work', timeLeft: '18h left', streakDays: 0, participants: 143, mood: 'amber' },
  ];

  stories: StoryCard[] = [
    { id: 's1', promptId: 'p1', user: 'Maths', timeAgo: '2h ago', reactions: 64, remixes: 7, badge: 'Remix-ready', reactionsLabel: '64', remixesLabel: '7', reactLabel: 'React', remixLabel: 'Remix', extraSubtopics: [] },
    { id: 's2', promptId: 'p1', user: 'Luis', timeAgo: '3h ago', reactions: 48, remixes: 3, reactionsLabel: '48', remixesLabel: '3', reactLabel: 'React', remixLabel: 'Remix', extraSubtopics: [] },
    { id: 's3', promptId: 'p1', user: 'Zoe', timeAgo: '10m ago', reactions: 18, remixes: 1, reactionsLabel: '18', remixesLabel: '1', reactLabel: 'React', remixLabel: 'Remix', extraSubtopics: [] },
  ];

  selectedPromptId = this.prompts[0].id;

  private schoolId?: string | null;
  public currentSubjectId?: string | null;
  public teacherSubjects: Array<{ subjectId: string; name: string; stage?: number; description?: string | null }> = [];
  public authUserId?: string | null;
  // expose for template
  public schoolName?: string | null;
  public schoolLogoUrl?: string | null;
  public teacherName?: string | null;
  public assignedLevel?: string | null;
  public studentCount: number = 0;
  public showStudents = false;
  public isHeadTeacher: boolean = false;
  public isTeacher: boolean = false;
  public students: Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }> = [];
  // parent topics loaded from server (not necessarily attached to a local story yet)
  public loadedParentTopics: Array<{ topicId?: string; name?: string | null; subtopic?: string | null; notes?: string | null }> = [];
  // cache of subjects with topics for the selected teacher (helpful to infer subject when needed)
  public teacherSubjectsWithTopics: Array<{ subjectId: string; name: string; topics: Array<any> }> = [];
  
  @ViewChildren('extraSubInput') extraInputs!: QueryList<ElementRef<HTMLInputElement>>;
  @ViewChild('subtopicStrip') subtopicStrip?: ElementRef<HTMLElement>;

  constructor(private api: ApiService, private toastCtrl: ToastController, private popCtrl: PopoverController, private modalCtrl: ModalController, private alertCtrl: AlertController, private router: Router) {
    addIcons({ flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline, logOutOutline, closeOutline, bookOutline });
  }

  // Simple heuristic to determine if an id is a server-generated GUID (not a local optimistic id like 's3')
  private isServerId(id: string): boolean {
    try {
      return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(String(id));
    } catch { return false; }
  }

  // Generate a stable, safely-unique local id for optimistic/local-only stories
  private generateLocalId(): string {
    // Prefer the browser-native UUID if available; otherwise fall back to a compact timestamp+random suffix
    try {
      // modern browsers expose crypto.randomUUID()
      const rnd = (typeof (window as any)?.crypto?.randomUUID === 'function') ? (window as any).crypto.randomUUID() : null;
      if (rnd) return `local-${rnd}`;
    } catch {}
    return `local-${Date.now().toString(36)}-${Math.random().toString(36).slice(2,9)}`;
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
      const r = await firstValueFrom(this.api.clearTopicsForSchool(this.schoolId));
      // Remove any references to server topics from local stories
      for (const story of this.stories) {
        // clear story.topicId
        if (story.topicId) story.topicId = undefined;
        if (Array.isArray(story.extraSubtopics)) {
          story.extraSubtopics = story.extraSubtopics.filter(s => !s || !s.topicId).map(s => ({ ...s, topicId: undefined }));
        }
      }
      try { window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { scope: 'cleared', count: r?.count ?? 0 } })); } catch {}
      try { window.dispatchEvent(new CustomEvent('topics:cleared', { detail: { count: r?.count ?? 0 } })); } catch {}
      const t = await this.toastCtrl.create({ message: `Cleared ${r?.count ?? 0} topic(s)`, duration: 1800, color: 'success' });
      await t.present();
    } catch (err) {
      console.error('Failed to clear topics', err);
      const t = await this.toastCtrl.create({ message: 'Failed to clear topics', duration: 2000, color: 'danger' });
      await t.present();
    }
  };

  async openStudentsModal() {
    try {
      // Ensure we have a fresh list of learners (use teacher-accessible endpoint)
      if (this.schoolId) {
        try {
          const students = await firstValueFrom(this.api.getLearners(this.schoolId));
          this.students = students ?? [];
          this.studentCount = Array.isArray(students) ? students.length : (students as any)?.length ?? 0;
          // Prepare a default display list of up to 10 learners (fill with placeholders if fewer)
          const top = (this.students || []).slice(0, 10);
          // proceed to present modal below
        } catch (err) {
          // fallback to older endpoint
          try { const s = await firstValueFrom(this.api.getStudents(this.schoolId!)); this.students = s ?? []; this.studentCount = (s || []).length; } catch { this.students = []; }
        }
      }
      // default: present modal with all students returned above
      const m = await this.modalCtrl.create({ component: StudentListModalComponent, componentProps: { students: this.students, schoolId: this.schoolId } });
      await m.present();
    } catch (err) {
      console.warn('Failed to open students modal', err);
    }
  }

  // Polling for external changes
  private refreshIntervalMs = 20000; // 20s
  private pollHandle: any = null;
  // Auto-open learners modal when learner count is small
  private readonly AUTO_OPEN_LEARNERS_THRESHOLD = 5;
  private modalAutoOpened = false;

  // Ask the user for their password before allowing them to close/remove a feed/story.
  async confirmClose(story: StoryCard): Promise<void> {
    try {
      // Only HeadTeacher may delete server-side topics. If caller is not headteacher, block early.
      const currentAuth = this.api.loadAuth();
      const roles = Array.isArray(currentAuth?.roles) ? (currentAuth!.roles as string[]) : [];
      const isHead = roles.some(r => String(r).toLowerCase() === 'headteacher');
      const isTeacher = roles.some(r => String(r).toLowerCase() === 'teacher');
      // Only teachers may delete server-side topics from their page. HeadTeacher is restricted.
      if (isHead) {
        const t = await this.toastCtrl.create({ message: 'Head Teacher is not permitted to delete topics here.', duration: 2500, color: 'warning' });
        await t.present();
        return;
      }
      if (!isTeacher) {
        const t = await this.toastCtrl.create({ message: 'Only teachers may delete feeds from this page.', duration: 2500, color: 'warning' });
        await t.present();
        return;
      }
      const alert = await this.alertCtrl.create({
        header: 'Confirm close',
        message: 'Enter your password to close this feed',
        inputs: [
          { name: 'password', type: 'password', placeholder: 'Password' }
        ],
        buttons: [
          { text: 'Cancel', role: 'cancel' },
          { text: 'Close feed', role: 'confirm' }
        ]
      });
      await alert.present();
      const res = await alert.onDidDismiss();
      if (res.role !== 'confirm') return;
      const password = (res?.data as any)?.values?.password ?? (res?.data as any)?.password ?? null;
      if (!password) {
        const t = await this.toastCtrl.create({ message: 'Password required', duration: 1500, color: 'warning' });
        await t.present();
        return;
      }

      const auth = this.api.loadAuth();
      const username = auth?.username ?? null;
      if (!username) {
        const t = await this.toastCtrl.create({ message: 'No authenticated user found', duration: 2000, color: 'danger' });
        await t.present();
        return;
      }

      // Verify credentials by attempting login. On success, persist returned auth and proceed with removal.
      this.api.login({ username, password }).subscribe({
        next: async (res) => {
          try { this.api.saveAuth(res); } catch {}
          // If this story is backed by a server Topic row, attempt to delete it from the DB.
          const schoolId = this.schoolId ?? (this.api.loadAuth() as any)?.schoolId ?? null;
          const subjectId = this.currentSubjectId ?? null;
          const topicId = story.topicId ?? null;
          if (schoolId && subjectId) {
            // Collect all topicIds associated with this story (story.topicId + any extraSubtopics with topicId)
            const toDelete = new Set<string>();
            if (topicId) toDelete.add(topicId);
            if (Array.isArray(story.extraSubtopics)) {
              for (const es of story.extraSubtopics) {
                if (es && es.topicId) toDelete.add(es.topicId as string);
              }
            }

            if (toDelete.size === 0) {
              // Nothing to delete server-side — just remove locally
              this.removeStory(story);
              const t = await this.toastCtrl.create({ message: 'Feed closed', duration: 1400, color: 'success' });
              await t.present();
            } else {
              // Attempt sequential deletes and collect failures
              const failures: Array<{ topicId: string; error: any }> = [];
              for (const id of Array.from(toDelete)) {
                try {
                  const r = await firstValueFrom(this.api.deleteTopic(schoolId, subjectId, id));
                  const deleted = (r && Array.isArray((r as any).deletedIds) && (r as any).deletedIds.length) ? (r as any).deletedIds : [id];
                  // dispatch deleted event for each deleted id
                  for (const did of deleted) {
                    try { window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { subjectId, topicId: did } })); } catch {}
                    // remove any ids that were cascaded from toDelete so we don't attempt to delete them again
                    if (toDelete.has(did)) toDelete.delete(did);
                  }
                } catch (err) {
                  failures.push({ topicId: id, error: err });
                }
              }

              // Remove story locally regardless of server delete outcome, but report failures
              this.removeStory(story);
              if (failures.length === 0) {
                const t = await this.toastCtrl.create({ message: 'Feed closed', duration: 1400, color: 'success' });
                await t.present();
              } else {
                console.error('Failed to delete some topics on server', failures);
                const t = await this.toastCtrl.create({ message: `Closed, but failed to delete ${failures.length} topic(s) on server`, duration: 3000, color: 'warning' });
                await t.present();
              }
            }
          } else {
            // No school/subject context — perform local removal
            this.removeStory(story);
            const t = await this.toastCtrl.create({ message: 'Feed closed', duration: 1400, color: 'success' });
            await t.present();
          }
        },
        error: async (err) => {
          console.error('Password verify failed', err);
          const t = await this.toastCtrl.create({ message: 'Incorrect password', duration: 1800, color: 'danger' });
          await t.present();
        }
      });
    } catch (err) {
      console.error('confirmClose failed', err);
    }
  }

    private topicsCreatedListener = (ev: any) => {
      try {
      const d = ev?.detail;
      if (!d) return;
      const sub = d.subtopic ?? '';
      const topicId = d.topicId ?? null;
      const subjectId = d.subjectId ?? null;
      const schoolId = d.schoolId ?? null;
      // If we already have a story for this subject that contains this topicId (as a parent or child), update it instead
      if (topicId) {
        // try to find a story where topicId matches parent or where an extraSubtopic has this topicId
        let found = this.stories.find(s => (s.subjectId ?? null) === (subjectId ?? this.currentSubjectId ?? null) && (s.topicId === topicId || (Array.isArray(s.extraSubtopics) && s.extraSubtopics.some(es => es.topicId === topicId))));
        // If not found by id, try to match by text (story.user or subtopic text) to handle cases where subjectId wasn't set yet in the local story
        if (!found) {
              found = this.stories.find(s => (s.subjectId ?? null) === (subjectId ?? this.currentSubjectId ?? null) && (!s.id || !this.isServerId(s.id)) && ((String(s.user || '').trim() === String(sub || '').trim()) || (Array.isArray(s.extraSubtopics) && s.extraSubtopics.some(es => String(es.subtopic || '').trim() === String(sub || '').trim()))));
        }
        if (found) {
          // ensure extraSubtopics contains this topic entry
          if (!found.extraSubtopics) found.extraSubtopics = [];
          const existing = found.extraSubtopics.find(es => es.topicId === topicId);
          if (!existing) found.extraSubtopics.unshift({ topicId: topicId as any, subtopic: sub });
          // ensure the story is associated with the subject when possible
          if (!found.subjectId && subjectId) found.subjectId = subjectId;
          this.saveStories();
          return;
        }
      }

      // Don't add topics that belong to a different subject than the current one
      if (subjectId && this.currentSubjectId && String(subjectId) !== String(this.currentSubjectId)) return;

      // Prevent duplicates: if any story already references this topicId or same subtopic in same subject, skip
      const already = (topicId && this.stories.some(s => (s.extraSubtopics || []).some(es => es && es.topicId === topicId))) || this.stories.some(s => (s.extraSubtopics || []).some(es => es && es.subtopic === sub && (s.subjectId ?? this.currentSubjectId ?? null) === (subjectId ?? this.currentSubjectId ?? null)));
      if (already) return;

      // If there's an existing local story with no server topic yet but whose name matches the created subtopic, attach the topic to that story
      // This avoids creating a duplicate pill when a local 'New subtopic' is saved to the server from another UI flow.
      if (topicId) {
        // Only match by name against local (non-server) stories to avoid incorrect attachments
        const nameMatch = this.stories.find(s => (!s.id || !this.isServerId(s.id)) && (!s.topicId) && ((String(s.user || '').trim() === String(sub || '').trim()) || (Array.isArray(s.extraSubtopics) && s.extraSubtopics.some(es => String(es.subtopic || '').trim() === String(sub || '').trim()))) && ((s.subjectId ?? null) === (subjectId ?? this.currentSubjectId ?? null) || !s.subjectId));
        if (nameMatch) {
          // ensure extraSubtopics contains this topic entry
          if (!nameMatch.extraSubtopics) nameMatch.extraSubtopics = [];
          const existing = nameMatch.extraSubtopics.find(es => es.topicId === topicId);
          if (!existing) nameMatch.extraSubtopics.unshift({ topicId: topicId as any, subtopic: sub });
          if (!nameMatch.subjectId && subjectId) nameMatch.subjectId = subjectId;
          // also, if the story had no canonical user set, prefer the server name
          if (!nameMatch.user || !String(nameMatch.user).trim()) nameMatch.user = sub || nameMatch.user;
          this.saveStories();
          return;
        }
      }

      const newStory: StoryCard = {
        id: this.generateLocalId(),
        promptId: this.selectedPromptId,
        subjectId: subjectId ?? this.currentSubjectId ?? null,
        user: this.teacherName ?? 'Teacher',
        timeAgo: 'just now',
        reactions: 0,
        remixes: 0,
        reactionsLabel: '0',
        remixesLabel: '0',
        reactLabel: 'React',
        remixLabel: 'Remix',
        extraSubtopics: [{ topicId: topicId ?? undefined, subtopic: sub }],
        // mark as freshly-local so we can avoid accidental immediate matching with server topics
        _localCreatedAt: Date.now(),
      };
      this.stories = [newStory, ...this.stories];
      this.saveStories();
    } catch (err) {
      console.warn('topics:created handler failed', err);
    }
  }

  private topicsUpdatedListener = (ev: any) => {
    try {
      const d = ev?.detail;
      if (!d) return;
      const topicId = d.topicId ?? null;
      const sub = d.subtopic ?? d.subtopic ?? '';
      // update any story that contains this topicId
      for (const story of this.stories) {
        if (!story.extraSubtopics) continue;
        for (const subObj of story.extraSubtopics) {
          if (subObj.topicId && subObj.topicId === topicId) {
            if (sub) subObj.subtopic = sub;
          }
        }
      }
      this.saveStories();
    } catch (err) { console.warn('topics:updated handler failed', err); }
  };

  async openSchoolPopover(ev: Event): Promise<void> {
    // Present a frontend-only performance chat modal when the logo is clicked.
    try {
      const modal = await this.modalCtrl.create({
        component: ChatPerformanceComponent,
        componentProps: { schoolId: this.schoolId, students: this.students },
        cssClass: 'performance-chat-modal'
      });
      await modal.present();
    } catch (err) {
      console.warn('Failed to open performance chat', err);
    }
  }

  ngOnInit(): void {
    this.loadStories();
    const auth = this.api.loadAuth();
    this.schoolId = auth?.schoolId ?? null;
    this.schoolName = auth?.schoolName ?? null;
    this.schoolLogoUrl = (auth as any)?.schoolLogoUrl ?? null;
    // teacher display name: prefer first/last if present, fall back to username
    this.teacherName = (auth as any)?.firstName || (auth as any)?.username || null;
    if (this.schoolId) {
      // load student count for the school to display in the hero
      // prefer the teacher-accessible learners endpoint which returns true counts for teachers
      this.api.getLearners(this.schoolId).subscribe({
        next: students => {
          this.studentCount = Array.isArray(students) ? students.length : (students as any)?.length ?? 0;
          // Auto-open learners modal for very small classes to surface the list immediately
          if (!this.modalAutoOpened && (this.studentCount ?? 0) <= this.AUTO_OPEN_LEARNERS_THRESHOLD) {
            this.modalAutoOpened = true;
            // slight delay to let the page render before showing the modal
            setTimeout(() => void this.openStudentsModal(), 220);
          }
        },
        error: () => {
          // fallback to head-only endpoint; if that also fails, show 0 (avoid displaying static placeholder counts)
          this.api.getStudents(this.schoolId!).subscribe({ next: students => { this.studentCount = Array.isArray(students) ? students.length : (students as any)?.length ?? 0; if (!this.modalAutoOpened && (this.studentCount ?? 0) <= this.AUTO_OPEN_LEARNERS_THRESHOLD) { this.modalAutoOpened = true; setTimeout(() => void this.openStudentsModal(), 220); } }, error: () => { this.studentCount = 0; } });
        }
      });
      // Prefer the subjects assigned to this teacher (if available) so topics are tied to teacher's subject
      const auth = this.api.loadAuth();
      const teacherId = auth?.userId ?? null;
      if (teacherId) {
        this.api.getTeacherSubjects(this.schoolId!, teacherId!).subscribe({
          next: subs => {
            this.teacherSubjects = subs ?? [];
            if (this.teacherSubjects.length === 1) {
              // single assigned subject — default to it
              this.currentSubjectId = this.teacherSubjects[0].subjectId;
              this.loadTopicsForCurrentSubject();
              // prefetch topics across teacher subjects to aid subject inference on delete
              void this.refreshTeacherSubjectsWithTopics();
            } else if (this.teacherSubjects.length > 1) {
              // multiple assigned subjects — require explicit selection by teacher
              this.currentSubjectId = null;
              void this.refreshTeacherSubjectsWithTopics();
            } else {
              // no assigned subjects — fall back to all subjects
              this.api.getSubjects(this.schoolId!).subscribe({ next: all => { if (all && all.length) { this.currentSubjectId = all[0].subjectId; this.loadTopicsForCurrentSubject(); } }, error: () => {} });
            }
          },
          error: () => {
            // on error, fall back to all subjects
            this.api.getSubjects(this.schoolId!).subscribe({ next: all => { if (all && all.length) { this.currentSubjectId = all[0].subjectId; this.loadTopicsForCurrentSubject(); } }, error: () => {} });
          }
        });
        // fetch membership to show assigned level (Stage) in header
        this.api.getMembership(this.schoolId!, teacherId!).subscribe({
          next: (m) => {
            const stageId = (m as any)?.stageId ?? null;
            if (stageId) {
              this.api.getStages(this.schoolId!).subscribe({ next: (stages) => {
                try {
                  const found = (stages || []).find(s => s.stageId === stageId);
                  if (found) this.assignedLevel = found.label ?? found.name;
                } catch {}
              }, error: () => {} });
            }
          },
          error: () => {
            // ignore membership errors
          }
        });
      } else {
        // no auth/teacher id — fall back to all subjects
        this.api.getSubjects(this.schoolId!).subscribe({ next: all => { if (all && all.length) { this.currentSubjectId = all[0].subjectId; this.loadTopicsForCurrentSubject(); } }, error: () => {} });
      }
    }
    this.authUserId = auth?.userId ?? null;
    // determine if current user is HeadTeacher for UI controls
    try {
      const roles = Array.isArray(auth?.roles) ? (auth!.roles as string[]) : [];
      this.isHeadTeacher = roles.some(r => String(r).toLowerCase() === 'headteacher');
      this.isTeacher = roles.some(r => String(r).toLowerCase() === 'teacher');
    } catch { this.isHeadTeacher = false; }
    // listen for topics created/updated elsewhere (popover/modal)
      try { window.addEventListener('topics:created', this.topicsCreatedListener); window.addEventListener('topics:updated', this.topicsUpdatedListener); window.addEventListener('topics:deleted', this.topicsDeletedListener); } catch {}

    // start polling for external changes when teacher page is active
    this.startPolling();
  }

  ngOnDestroy(): void {
      try { window.removeEventListener('topics:created', this.topicsCreatedListener); window.removeEventListener('topics:updated', this.topicsUpdatedListener); window.removeEventListener('topics:deleted', this.topicsDeletedListener); } catch {}
    this.stopPolling();
  }

  // Refresh teacherSubjectsWithTopics by fetching topics for each assigned subject (best-effort)
  private async refreshTeacherSubjectsWithTopics(): Promise<void> {
    if (!this.schoolId) return;
    try {
      const auth = this.api.loadAuth();
      const teacherId = auth?.userId ?? null;
      if (!teacherId) return;
      const subs = await firstValueFrom(this.api.getTeacherSubjects(this.schoolId, teacherId));
      const arr = Array.isArray(subs) ? subs as any[] : [];
      const out: Array<{ subjectId: string; name: string; topics: Array<any> }> = [];
      for (const s of arr) {
        const subjectId = s.subjectId;
        const name = s.name || '';
        try {
          const topics = await firstValueFrom(this.api.getTopics(this.schoolId!, subjectId));
          const tarr = Array.isArray(topics) ? topics : [];
          out.push({ subjectId, name, topics: tarr });
        } catch {
          out.push({ subjectId, name, topics: [] });
        }
      }
      this.teacherSubjectsWithTopics = out;
    } catch (err) {
      console.warn('Failed to refresh teacher subjects with topics', err);
      this.teacherSubjectsWithTopics = [];
    }
  }

  private startPolling(): void {
    this.stopPolling();
    try {
      this.pollHandle = setInterval(() => this.refreshIfVisible(), this.refreshIntervalMs);
    } catch (e) {
      this.pollHandle = null;
    }
  }

  private stopPolling(): void {
    try { if (this.pollHandle) clearInterval(this.pollHandle); } catch {}
    this.pollHandle = null;
  }

  // Only refresh when the page is visible to the user to avoid unnecessary calls
  private refreshIfVisible(): void {
    try {
      if (typeof document !== 'undefined' && document.visibilityState && document.visibilityState !== 'visible') return;
      if (!this.schoolId || !this.currentSubjectId) return;
      this.loadTopicsForCurrentSubject();
    } catch (err) {
      // ignore polling errors
    }
  }

    private topicsDeletedListener = (ev: any) => {
      try {
        const d = ev?.detail;
        if (!d) return;
        const topicId = d.topicId ?? null;
        if (!topicId) return;
        let changed = false;
        // Remove stories that reference this topic as their parent topicId
        this.stories = this.stories.filter(s => {
          if (s.topicId && s.topicId === topicId) {
            changed = true;
            return false; // remove entire story
          }
          if (s.extraSubtopics && s.extraSubtopics.some(es => es.topicId === topicId)) {
            // remove matching subtopics
            s.extraSubtopics = s.extraSubtopics.filter(es => es.topicId !== topicId);
            changed = true;
            // if story no longer has any subtopics and doesn't have a topicId, remove the story entirely
            if ((s.extraSubtopics?.length ?? 0) === 0 && !s.topicId) return false;
          }
          return true;
        });
        if (changed) this.saveStories();
      } catch (err) { console.warn('topics:deleted handler failed', err); }
    };

  async openNotes(topicId?: string | null, subjectId?: string | null, subTopicId?: string | null): Promise<void> {
    if (!topicId || !subjectId) return;
    const auth = this.api.loadAuth();
    const schoolId = auth?.schoolId ?? null;
    // Present notes as a full-screen modal so teachers have the full notes UI
    const props: any = { topicId, subjectId, schoolId };
    if (subTopicId) props.subTopicId = subTopicId;
    const m = await this.modalCtrl.create({ component: CreateNotePage, componentProps: props, cssClass: 'full-screen-modal' });
    await m.present();
  }

  // Ensure a server topic exists for a story's subtopic and then open notes for it.
  async handleNotesClick(sub: any, story: StoryCard, ev?: Event): Promise<void> {
    try {
      if (ev) ev.stopPropagation();
      const subjectId = this.currentSubjectId ?? null;
      const schoolId = this.schoolId ?? this.api.loadAuth()?.schoolId ?? null;
      if (!subjectId || !schoolId) {
        const t = await this.toastCtrl.create({ message: 'Select a subject first', duration: 1800, color: 'warning' });
        await t.present();
        return;
      }

      // If topic already exists, open notes directly. If we have a subTopicId, prefer that.
      if (sub && sub.topicId) {
        await this.openNotes(sub.topicId, subjectId, (sub as any).subTopicId ?? null);
        return;
      }

      // Avoid duplicate creates
      if (sub && (sub as any)._creating) return;
      if (sub) (sub as any)._creating = true;

      // If the story already has a parent topic, create a child Topic row (no Subtopic) then a SubTopic entry.
      if (story.topicId) {
        try {
          // 1) create a child Topic row without Subtopic so backend doesn't auto-create a SubTopic
          const createChildTopicPayload: any = { Subtopic: null, Notes: null, TopicName: null, ParentTopicId: story.topicId };
          const childTopicRes = await firstValueFrom(this.api.createTopic(schoolId, subjectId, createChildTopicPayload));
          const childTopicId = childTopicRes?.topicId ?? null;
          if (!childTopicId) {
            const t = await this.toastCtrl.create({ message: 'Failed to create topic', duration: 2000, color: 'danger' }); await t.present();
            return;
          }
          // 2) create the SubTopic entry that references the child Topic row
          try {
            const stRes = await firstValueFrom(this.api.createSubTopic(schoolId, subjectId, childTopicId, { Name: sub?.subtopic ?? '', Notes: null }));
            const subTopicId = stRes?.subTopicId ?? null;
            if (subTopicId) {
              // attach both ids for future edits/deletes
              sub.topicId = childTopicId;
              (sub as any).subTopicId = subTopicId;
              story.subjectId = subjectId ?? story.subjectId ?? null;
              this.saveStories();
              try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: childTopicId, subtopic: sub?.subtopic ?? '', subjectId, schoolId } })); } catch {}
              await this.openNotes(childTopicId, subjectId, subTopicId);
              return;
            }
          } catch (err) {
            console.error('Failed to create SubTopic for notes', err);
            // If SubTopic creation failed, fallback to opening notes on the child Topic row
            sub.topicId = childTopicId;
            story.subjectId = subjectId ?? story.subjectId ?? null;
            this.saveStories();
            try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: childTopicId, subtopic: sub?.subtopic ?? '', subjectId, schoolId } })); } catch {}
            await this.openNotes(childTopicId, subjectId);
            return;
          }
        } catch (err) {
          console.error('Failed to create child topic for notes', err);
          const t = await this.toastCtrl.create({ message: 'Failed to create topic', duration: 2000, color: 'danger' }); await t.present();
          return;
        }
      }

      // No parent exists: create parent topic, then create a child subtopic and open notes for the subtopic
      try {
        // Create parent with canonical TopicName (no Subtopic so we ensure subtopic is a child)
        const parentPayload: any = { Subtopic: null, Notes: null, TopicName: story.user ?? sub?.subtopic ?? null };
        const parentRes = await firstValueFrom(this.api.createTopic(schoolId, subjectId, parentPayload));
        const parentId = parentRes?.topicId ?? null;
        if (!parentId) {
          const t = await this.toastCtrl.create({ message: 'Failed to create topic', duration: 2000, color: 'danger' }); await t.present();
          return;
        }
        // Immediately set the local story's canonical topic name so inline editing doesn't get reverted
        if (story && (story.user || '') !== (parentPayload.TopicName || '')) {
          story.user = parentPayload.TopicName ?? story.user;
          (story as any)._savedTopic = story.user;
          this.saveStories();
        }

        // Now create the child subtopic row so notes can attach to the subtopic specifically
        try {
          const childPayload: any = { Subtopic: null, Notes: null, TopicName: null, ParentTopicId: parentId };
          const childRes = await firstValueFrom(this.api.createTopic(schoolId, subjectId, childPayload));
          const childId = childRes?.topicId ?? null;
          // If child creation succeeded, attach child to sub and then create SubTopic record for it
          if (childId) {
            // create SubTopic entry referencing the child Topic row
            try {
              const stRes = await firstValueFrom(this.api.createSubTopic(schoolId, subjectId, childId, { Name: sub?.subtopic ?? '', Notes: null }));
              const subTopicId = stRes?.subTopicId ?? null;
              if (subTopicId) (sub as any).subTopicId = subTopicId;
            } catch (err) {
              console.error('Failed to create SubTopic after parent creation', err);
            }
            story.topicId = parentId;
            sub.topicId = childId;
            // ensure story is associated with this subject
            story.subjectId = subjectId ?? story.subjectId ?? null;
            this.saveStories();
            // persist story.topicId to server-backed story if available
            if (story.id && this.isServerId(story.id) && this.schoolId && subjectId) {
              this.api.updateStory(this.schoolId, subjectId, story.id, { topicId: parentId }).subscribe({ next: () => {}, error: (e) => console.warn('Failed to persist story.topicId on create child', e) });
            }
            try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: childId, subtopic: sub?.subtopic ?? '', subjectId, schoolId } })); } catch {}
            await this.openNotes(childId, subjectId);
            return;
          }
          // If child creation failed but parent exists, fallback to opening notes on parent
          story.topicId = parentId;
          sub.topicId = parentId;
          story.subjectId = subjectId ?? story.subjectId ?? null;
          this.saveStories();
          if (story.id && this.isServerId(story.id) && this.schoolId && subjectId) {
            this.api.updateStory(this.schoolId, subjectId, story.id, { topicId: parentId }).subscribe({ next: () => {}, error: (e) => console.warn('Failed to persist story.topicId on parent fallback', e) });
          }
          try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: parentId, subtopic: sub?.subtopic ?? '', subjectId, schoolId } })); } catch {}
          await this.openNotes(parentId, subjectId);
          return;
        } catch (err) {
          console.error('Failed to create child subtopic for notes', err);
          // fallback: open notes on parent
          story.topicId = parentId;
          sub.topicId = parentId;
          story.subjectId = subjectId ?? story.subjectId ?? null;
          this.saveStories();
          try { window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: parentId, subtopic: sub?.subtopic ?? '', subjectId, schoolId } })); } catch {}
          await this.openNotes(parentId, subjectId);
          return;
        }
      } catch (err) {
        console.error('Failed to create parent topic for notes', err);
        const t = await this.toastCtrl.create({ message: 'Failed to create topic', duration: 2000, color: 'danger' }); await t.present();
        return;
      }
    } finally {
      if (sub) (sub as any)._creating = false;
    }
  }
  
  openTopicPerformance(subjectId?: string | null, topicId?: string | null) {
    if (!subjectId || !topicId) return;
    // navigate to the dedicated topic performance page
    this.router.navigateByUrl(`/subjects/${subjectId}/topics/${topicId}/performance`);
  }

  toggleStudents(ev?: Event): void {
    if (ev) ev.stopPropagation();
    // toggle visible state; if opening and no students loaded, fetch them
    const opening = !this.showStudents;
    this.showStudents = opening;
    if (opening && this.students.length === 0 && this.schoolId) {
      // Use getLearners (teacher-accessible) to get a truthful list & count of learners
      this.api.getLearners(this.schoolId).subscribe({
        next: students => {
          this.students = students ?? [];
          this.studentCount = Array.isArray(students) ? students.length : (students as any)?.length ?? 0;
          
        },
        error: () => {
          // fallback to older head-only endpoint if learners fails
            this.api.getStudents(this.schoolId!).subscribe({ next: s => { this.students = s ?? []; this.studentCount = (s || []).length; }, error: () => { this.students = []; } });
        }
      });
    }
  }

  openLearnerPerformance(student: { userId?: string; username?: string; firstName?: string | null; lastName?: string | null }) {
    if (!student?.userId) return;
    this.showStudents = false;
    this.router.navigateByUrl(`/learner/${student.userId}`);
  }

  studentDisplayName(s: { firstName?: string | null; lastName?: string | null; username?: string | null }): string {
    const parts: string[] = [];
    if (s.firstName) parts.push(s.firstName);
    if (s.lastName) parts.push(s.lastName);
    if (parts.length) return parts.join(' ');
    return s.username ?? 'Learner';
  }

  selectPrompt(id: string): void {
    this.selectedPromptId = id;
  }

  storiesForPrompt(): StoryCard[] {
    // Show stories matching current prompt and (when a subject is selected) matching that subject.
    return this.stories.filter(s => s.promptId === this.selectedPromptId && (this.currentSubjectId ? s.subjectId === this.currentSubjectId : true));
  }

  // Clicking Record / Upload immediately creates a new story (no modal or draft)
  addStory(): void {
    const mock: StoryCard = {
      id: this.generateLocalId(),
      promptId: this.selectedPromptId,
      subjectId: this.currentSubjectId ?? null,
      user: 'Topic Name',
      timeAgo: 'just now',
      reactions: 0,
      remixes: 0,
      badge: 'New',
      reactionsLabel: '0',
      remixesLabel: '0',
      reactLabel: 'React',
      remixLabel: 'Remix',
      editingReactions: false,
      editingRemixes: false,
      editingReact: false,
      editingRemix: false,
      editingName: false,
      reactionsSubtopic: '',
      remixesSubtopic: '',
      reactSubtopic: '',
      remixSubtopic: '',
      extraSubtopics: [],
      // mark as freshly-local so we can avoid accidental immediate matching with server topics
      _localCreatedAt: Date.now()
    };

    // Optimistically add locally for immediate UX
    this.stories = [mock, ...this.stories];
    this.saveStories();

    // Persist server-side when we have school/subject context
    const schoolId = this.schoolId ?? (this.api.loadAuth() as any)?.schoolId ?? null;
    const subjectId = this.currentSubjectId ?? null;
    if (schoolId && subjectId) {
      try {
        this.api.createStory(schoolId, subjectId, { promptId: mock.promptId, user: mock.user, payload: null }).subscribe({
          next: (res) => {
            // Replace optimistic id with server-backed id
              const serverId = (res as any).storyId as any;
              mock.id = serverId;
              mock.badge = undefined;
              // ensure story persisted has server subject
              mock.subjectId = subjectId;
              // clear local-created marker so it can be matched or updated normally later
              try { delete (mock as any)._localCreatedAt; } catch {}
              this.saveStories();
              // If a topic was created earlier (race), persist topicId to the newly-created story row
              if (mock.topicId && this.schoolId && subjectId) {
                try { this.api.updateStory(this.schoolId, subjectId, serverId, { topicId: mock.topicId }).subscribe({ next: () => {}, error: (e) => console.warn('Failed to persist story.topicId after story create', e) }); } catch (e) { console.warn('Failed to persist story.topicId after story create', e); }
              }
          },
          error: (err) => {
            console.error('Failed to create story on server', err);
          }
        });
      } catch (err) {
        console.error('createStory failed', err);
      }
    }
  }

  // Save the story-level topic (create Topic record) so subtopics/notes can be attached
  saveTopic(story: StoryCard, _retryOnce: boolean = false): void {
    const topicName = (story.user || '').trim();
    if (!topicName) {
      this.toastCtrl.create({ message: 'Please enter a topic name', duration: 1500, color: 'warning' }).then(t => t.present());
      return;
    }

    // Try to resolve schoolId / subjectId from current state or auth payload
    let schoolId = this.schoolId ?? (this.api.loadAuth() as any)?.schoolId ?? null;
    let subjectId = this.currentSubjectId ?? null;

    // If we don't have a subjectId but have a schoolId, attempt to load subjects and retry once
    if (schoolId && !subjectId && !_retryOnce) {
      this.api.getSubjects(schoolId).subscribe({
        next: subs => {
          if (subs && subs.length) {
            this.currentSubjectId = subs[0].subjectId;
            // retry save with resolved subject
            this.saveTopic(story, true);
          } else {
            // no subjects available — fall back to local save
            this._saveTopicLocally(story, topicName);
          }
        },
        error: () => {
          // network error — fall back to local save
          this._saveTopicLocally(story, topicName);
        }
      });
      return;
    }

    // If still missing school or subject, do a local-only save (non-blocking)
    if (!schoolId || !this.currentSubjectId) {
      this._saveTopicLocally(story, topicName);
      return;
    }

    // Always create a new topic when Save is clicked on the story pill.
    // Use notes=null and include TopicName so backend stores canonical name.
    this.api.createTopic(schoolId, this.currentSubjectId, { subtopic: topicName, notes: null, TopicName: topicName }).subscribe({
      next: (res: any) => {
        const topicId = res?.topicId ?? null;
        // Store the returned topicId on the story so subtopics/notes can reference it later
        if (topicId) {
          story.topicId = topicId;
          // tag story with the current subject so it persists for that subject
          story.subjectId = this.currentSubjectId ?? story.subjectId ?? null;
          // persist story.topicId to server-backed story row when available
          if (story.id && this.isServerId(story.id) && this.schoolId && this.currentSubjectId) {
            this.api.updateStory(this.schoolId, this.currentSubjectId, story.id, { topicId }).subscribe({ next: () => {}, error: e => console.warn('Failed to persist story.topicId', e) });
          }
        }
        // clear the 'New' badge and persist
        story.badge = undefined;
        this.saveStories();
        this.toastCtrl.create({ message: 'Topic created', duration: 1500, color: 'success' }).then(t => t.present());
      },
      error: (err) => {
        console.error('Failed to create topic', err);
        this.toastCtrl.create({ message: 'Failed to create topic', duration: 2000, color: 'danger' }).then(t => t.present());
      }
    });
  }

  // Local fallback when backend can't be used — attach topic locally but with no topicId
  private _saveTopicLocally(story: StoryCard, topicName: string): void {
    if (!story.extraSubtopics) story.extraSubtopics = [];
    if (story.extraSubtopics.length === 0) {
      story.extraSubtopics.unshift({ subtopic: topicName });
    } else {
      story.extraSubtopics[0].subtopic = topicName;
    }
    story.badge = undefined;
    // persist which subject this local topic is for
    story.subjectId = this.currentSubjectId ?? story.subjectId ?? null;
    this.saveStories();
    this.toastCtrl.create({ message: 'Saved locally (no school/subject)', duration: 1800, color: 'warning' }).then(t => t.present());
  }

  addSubtopic(story: StoryCard): void {
    if (!story.extraSubtopics) { story.extraSubtopics = []; }
    // Push an empty object so we can keep topicId when created
    story.extraSubtopics.push({ subtopic: 'New subtopic' });
    // mark the newly-added index as editing so an input is shown
    story.editingExtraIndex = story.extraSubtopics.length - 1;
    this.saveStories();
    // focus the newly added native input (last input in the DOM)
    requestAnimationFrame(() => {
      try {
        const arr = this.extraInputs.toArray();
        const lastRef = arr[arr.length - 1];
        const el = lastRef?.nativeElement;
        if (el) {
          el.focus();
          el.select();
        }
      } catch (e) {
        // ignore focus/select errors
      }
      this.scrollSubtopicsStrip();
    });
  }

  editExtra(story: StoryCard, i: number): void {
    story.editingExtraIndex = i;
    // give Angular a tick to render the input, then focus
    requestAnimationFrame(() => {
      try {
        const arr = this.extraInputs.toArray();
        const ref = arr[i];
        const el = ref?.nativeElement;
        if (el) {
          el.focus();
          el.select();
        }
      } catch (e) {
        // ignore
      }
      this.scrollSubtopicsStrip();
    });
  }

  finishEditingExtra(story: StoryCard): void {
    story.editingExtraIndex = null;
    this.saveStories();
  }

  // Called when the teacher finishes editing the Topic name (press Enter or blur)
  onTopicEditDone(story: StoryCard): void {
    // close the inline editor and update local cache only
    story.editingName = false;
    // store the trimmed value locally to avoid unnecessary future saves
    const newTopicText = (story.user || '').trim();
    if (!newTopicText) return;
    (story as any)._savedTopic = newTopicText;
  }

  // Save a single extra subtopic (called on Enter in the extra-subtopic input)
  saveSubtopic(story: StoryCard, i: number): void {
    const obj = story.extraSubtopics?.[i];
    const value = obj?.subtopic?.trim() ?? '';
    if (!value) return; // nothing to save
    // If teacher has multiple assigned subjects, require explicit selection before creating server topics
    if ((this.teacherSubjects?.length ?? 0) > 1 && !this.currentSubjectId) {
      this.toastCtrl.create({ message: 'Select a subject first', duration: 2000, color: 'warning' }).then(t => t.present());
      return;
    }
    // If we have a topicId, update the existing topic, otherwise create a new one
    if (this.schoolId && this.currentSubjectId && obj) {
      const topicName = (story.user || '').trim();
      // Helper to collapse + toast
      const onSuccess = (msg: string) => this.collapseEditor(story, i, () => { this.saveStories(); this.toastCtrl.create({ message: msg, duration: 1500, color: 'success' }).then(t => t.present()); });
      const onFail = (msg: string) => this.collapseEditor(story, i, () => { this.saveStories(); console.error(msg, value); this.toastCtrl.create({ message: msg, duration: 2500, color: 'danger' }).then(t => t.present()); });

      // Helper to ensure the parent TopicName is saved (create parent if missing)
      const ensureParentTopicName = (cb: (okMsg?: string) => void, failMsg: (msg: string) => void) => {
        if (story.topicId) {
          this.api.updateTopic(this.schoolId!, this.currentSubjectId!, story.topicId!, { TopicName: topicName }).subscribe({ next: () => cb('Topic updated'), error: () => failMsg('Failed to update topic name') });
        } else {
          // create parent topic with Subtopic set to the saved subtopic value so we don't create an empty parent row
          const parentPayload = { Subtopic: value, Notes: null, TopicName: topicName || null };
          this.api.createTopic(this.schoolId!, this.currentSubjectId!, parentPayload).subscribe({
            next: (parentRes) => {
              const parentId = parentRes?.topicId ?? null;
              if (parentId) {
                story.topicId = parentId;
                cb('Parent topic created');
              } else {
                failMsg('Failed to create parent topic');
              }
            },
            error: () => failMsg('Failed to create parent topic')
          });
        }
      };

      // If this extra subtopic already maps to a DB row -> update child.
      // Prefer SubTopic table updates when we have a subTopicId recorded (newer flow).
      if (obj.topicId && (obj as any).subTopicId) {
        // update subtopic (child) record
        this.api.updateSubTopic(this.schoolId!, this.currentSubjectId!, story.topicId!, (obj as any).subTopicId, { Name: value }).subscribe({
          next: () => {
            // do NOT update parent topic name here — preserve parent's canonical name until the teacher explicitly edits it
            onSuccess('Subtopic updated');
          },
          error: () => onFail('Failed to update subtopic')
        });
        return;
      }

      // Legacy: if this extra subtopic maps to a Topic row (old behavior), update the Topic row as before
      if (obj.topicId && !(obj as any).subTopicId) {
        this.api.updateTopic(this.schoolId!, this.currentSubjectId!, obj.topicId!, { Subtopic: value, ParentTopicId: story.topicId ?? undefined }).subscribe({
          next: () => {
            ensureParentTopicName(() => onSuccess('Subtopic updated'), (m) => onFail(m));
          },
          error: () => onFail('Failed to update subtopic')
        });
        return;
      }

      // No topicId for this subtopic yet
      if (story.topicId) {
        // We have a parent — create a SubTopic child record (do NOT update parent name here).
        this.api.createSubTopic(this.schoolId!, this.currentSubjectId!, story.topicId, { Name: value }).subscribe({
          next: (res) => {
            if (res && (res as any).subTopicId) {
              // mark this extraSubtopic as backed by a SubTopic row
              (obj as any).subTopicId = (res as any).subTopicId;
              // keep topicId for legacy compatibility when needed, but prefer subTopicId for child ops
              obj.topicId = story.topicId; // parent ref retained for context
            }
            // ensure story is tied to the subject
            story.subjectId = this.currentSubjectId ?? story.subjectId ?? null;
            // do NOT call ensureParentTopicName to avoid re-saving the parent when creating subtopics
            onSuccess('Subtopic created');
          },
          error: () => onFail('Failed to create subtopic')
        });
        return;
      }

      // No parent exists yet: create parent first, then create child
      // Create parent with empty Subtopic and TopicName set when topicName exists;
      // otherwise create parent with Subtopic equal to the subtopic value.
      // Create a parent topic and use it as the subtopic record (avoid creating an empty parent + separate child)
      const initialParentPayload = { Subtopic: value, Notes: null, TopicName: topicName || null };
      this.api.createTopic(this.schoolId!, this.currentSubjectId!, initialParentPayload).subscribe({
        next: (parentRes) => {
          const parentId = parentRes?.topicId ?? null;
          if (parentId) {
            story.topicId = parentId;
            // record this subtopic as the newly-created parent row (no separate child created)
            obj.topicId = parentId;
            // ensure story is tied to the subject
            story.subjectId = this.currentSubjectId ?? story.subjectId ?? null;
            // set canonical topic name locally so edits don't revert
            if (topicName) {
              story.user = topicName;
              (story as any)._savedTopic = topicName;
            }
            // persist story.topicId to server if this story is already persisted
            if (story.id && this.isServerId(story.id) && this.schoolId && this.currentSubjectId) {
              this.api.updateStory(this.schoolId, this.currentSubjectId, story.id, { topicId: parentId }).subscribe({ next: () => {}, error: e => console.warn('Failed to persist story.topicId on initial parent create', e) });
            }
            ensureParentTopicName(() => onSuccess('Subtopic created'), (m) => onFail(m));
          } else {
            onFail('Failed to create parent topic');
          }
        },
        error: () => onFail('Failed to create parent topic')
      });
    } else {
      // no backend context — just save locally
      story.editingExtraIndex = null;
      this.saveStories();
    }
  }

  // Open the inline editor for a specific subtopic (alias to existing editExtra)
  openInlineEditor(story: StoryCard, i: number): void {
    this.editExtra(story, i);
  }

  // Cancel inline editing and reload server state for current subject to discard local edits
  cancelInline(story: StoryCard, i?: number): void {
    // animate collapse first, then revert to server state
    const idx = typeof i === 'number' ? i : (story.editingExtraIndex ?? 0);
    this.collapseEditor(story, idx, () => {
      // reload topics for the selected subject to revert local changes
      this.loadTopicsForCurrentSubject();
    });
  }

  // Collapse the inline editor with a short animation before clearing state
  private collapseEditor(story: StoryCard, index: number, cb?: () => void): void {
    try {
      // mark which index is collapsing so template keeps the editor in DOM
      (story as any).editorCollapsingIndex = index;
      // give CSS time to run transition then clear editing index
      setTimeout(() => {
        story.editingExtraIndex = null;
        (story as any).editorCollapsingIndex = null;
        if (cb) cb();
      }, 220);
    } catch (e) {
      // fallback: clear immediately
      story.editingExtraIndex = null;
      (story as any).editorCollapsingIndex = null;
      if (cb) cb();
    }
  }

  // Remove a subtopic locally (server-side delete not implemented). If a topicId existed, inform the user.
  deleteSubtopic(story: StoryCard, i: number): void {
    const obj = story.extraSubtopics?.[i];
    if (!story.extraSubtopics) return;
    const hadTopicId = !!(obj && obj.topicId);

    // If this subtopic is backed by a server topic row, attempt server delete when possible
    const schoolId = this.schoolId ?? (this.api.loadAuth() as any)?.schoolId ?? null;
    // Prefer the subject associated with the story itself (more accurate when deleting a subtopic)
    let resolvedSubjectId = story.subjectId ?? this.currentSubjectId ?? null;

    if (hadTopicId && schoolId) {
      // If we don't have a subject context, try to infer it from loaded teacher subjects
      if (!resolvedSubjectId && obj?.topicId && Array.isArray(this.teacherSubjectsWithTopics)) {
        for (const s of this.teacherSubjectsWithTopics) {
          if ((s.topics || []).some((t: any) => t.topicId === obj.topicId)) { resolvedSubjectId = s.subjectId; break; }
        }
      }

      if (!resolvedSubjectId) {
        // If we still don't have a subject context, avoid deleting on server and inform the user
        this.toastCtrl.create({ message: 'Cannot delete server topic: no subject selected', duration: 2000, color: 'warning' }).then(t => t.present());
        return;
      }
      const currentAuth = this.api.loadAuth();
      const roles = Array.isArray(currentAuth?.roles) ? (currentAuth!.roles as string[]) : [];
      const isHead = roles.some(r => String(r).toLowerCase() === 'headteacher');

      if (!isHead) {
        // Non-head teachers are allowed to delete SubTopic (child) rows, but not Topic rows.
        // If this extra subtopic is only represented by a Topic row (no subTopicId), do local-only removal.
        if (!(obj as any).subTopicId) {
          story.extraSubtopics.splice(i, 1);
          this.saveStories();
          this.toastCtrl.create({ message: 'Removed locally (server delete not available for topics)', duration: 1400 }).then(t => t.present());
          return;
        }
        // otherwise allow flow to continue and call DeleteSubTopic for authorized teachers
      }

      // Head teacher: call API to delete the topic (use resolvedSubjectId)
      // If this extra subtopic was created using the SubTopic API, call DeleteSubTopic; otherwise fall back to deleting Topic row
      if ((obj as any).subTopicId) {
        this.api.deleteSubTopic(schoolId, resolvedSubjectId, story.topicId!, (obj as any).subTopicId).subscribe({
          next: async () => {
            try {
              // remove locally
              story.extraSubtopics!.splice(i, 1);
              this.saveStories();
              // If a child Topic row exists for this subtopic, also delete it so topic-performance won't show it
              if (obj.topicId && String(obj.topicId) !== String(story.topicId)) {
                try {
                  const r = await firstValueFrom(this.api.deleteTopic(schoolId, resolvedSubjectId!, String(obj.topicId)));
                  // dispatch deleted events for any cascaded deletes returned by deleteTopic
                  const deleted = (r && Array.isArray((r as any).deletedIds) && (r as any).deletedIds.length) ? (r as any).deletedIds : [obj.topicId];
                  for (const did of deleted) {
                    try { window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { subjectId: resolvedSubjectId, topicId: did } })); } catch {}
                  }
                } catch (e) {
                  // best-effort: ignore child topic delete failures but still refresh lists
                  console.warn('Failed to delete child topic after subtopic delete', e);
                }
              }
              // refresh topics so UI updates
              try { await Promise.allSettled([this.loadTopicsForCurrentSubject(), this.refreshTeacherSubjectsWithTopics()]); } catch (e) { }
              const t = await this.toastCtrl.create({ message: 'Subtopic deleted', duration: 1400, color: 'success' });
              await t.present();
            } catch (err) {
              console.error('Failed to finalize subtopic delete', err);
            }
          },
          error: async (err) => {
            console.error('Failed to delete subtopic on server', err);
            const msg = err?.error?.message ?? err?.message ?? 'Failed to delete subtopic';
            const t = await this.toastCtrl.create({ message: msg, duration: 3000, color: 'danger' });
            await t.present();
          }
        });
        return;
      }

      this.api.deleteTopic(schoolId, resolvedSubjectId, obj!.topicId!).subscribe({
        next: async (res: any) => {
          try {
            // remove locally
            story.extraSubtopics!.splice(i, 1);
            this.saveStories();
            // dispatch deletion events for any cascaded deletes returned by the server
            const deleted = (res && Array.isArray(res.deletedIds) && res.deletedIds.length) ? res.deletedIds : [obj!.topicId];
            for (const did of deleted) {
              try { window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { subjectId: resolvedSubjectId, topicId: did } })); } catch {}
            }
            // refresh server-side caches so the UI doesn't re-show deleted topics after a reload
            try { await Promise.allSettled([this.loadTopicsForCurrentSubject(), this.refreshTeacherSubjectsWithTopics()]); } catch (e) { /* best-effort */ }
            const t = await this.toastCtrl.create({ message: 'Subtopic deleted', duration: 1400, color: 'success' });
            await t.present();
          } catch (err) {
            console.error('Failed to finalize subtopic delete', err);
          }
        },
        error: async (err) => {
          console.error('Failed to delete subtopic on server', err);
          const msg = err?.error?.message ?? err?.message ?? 'Failed to delete subtopic';
          const t = await this.toastCtrl.create({ message: msg, duration: 3000, color: 'danger' });
          await t.present();
        }
      });
      return;
    }

    // Fallback: no server side topic to delete — remove locally
    story.extraSubtopics.splice(i, 1);
    this.saveStories();
    this.toastCtrl.create({ message: hadTopicId ? 'Removed locally (server delete not available)' : 'Removed', duration: 1400 }).then(t => t.present());
  }

  toggleStoryEdit(story: StoryCard, pill: 'reactions' | 'remixes' | 'react' | 'remix'): void {
    switch (pill) {
      case 'reactions': story.editingReactions = !story.editingReactions; break;
      case 'remixes': story.editingRemixes = !story.editingRemixes; break;
      case 'react': story.editingReact = !story.editingReact; break;
      case 'remix': story.editingRemix = !story.editingRemix; break;
    }
    this.saveStories();
  }

  toggleNameEdit(story: StoryCard): void {
    story.editingName = !story.editingName;
    this.saveStories();
  }

  removeStory(story: StoryCard): void {
    this.stories = this.stories.filter(s => s.id !== story.id);
    this.saveStories();
  }

  public saveStories(): void {
    try {
      // Persist stories to localStorage but keep them "clean" by
      // removing any inline `notes` stored on extraSubtopics. Notes
      // should be persisted via the Topics API (createTopic/updateTopic)
      // and not embedded inside the story payload saved locally.
      const cleaned = this.stories.map(s => {
        // shallow clone story metadata
        const c: any = { ...s };
        if (Array.isArray(c.extraSubtopics)) {
          c.extraSubtopics = c.extraSubtopics.map((es: any) => {
            // strip notes before saving
            const { notes, ...rest } = es ?? {};
            return rest;
          });
        }
        return c;
      });
      localStorage.setItem(this.STORIES_KEY, JSON.stringify(cleaned));
    } catch (e) {
      // ignore storage errors (private/incognito mode)
      console.warn('Unable to save stories to localStorage', e);
    }
  }

  public async loadTopicsForCurrentSubject(): Promise<void> {
    if (!this.schoolId || !this.currentSubjectId) return;
    this.api.getTopics(this.schoolId, this.currentSubjectId).subscribe({
      next: async topics => {
        // map topics into existing stories for the selected prompt — append subtopics
        const forPrompt = this.storiesForPrompt();
        const topicIds = new Set((topics || []).map((t: any) => String(t.topicId)));
        // Remove any stories whose topicId no longer exists on the server
        let removed = false;
        // Do not remove or mutate local stories when the server topics list doesn't include their topicId.
        // A story should only be removed when explicitly deleted. Keep local state intact and rely on
        // hydration (matching by id or name) to attach server topics to local stories when available.
        // For existing server-backed stories, attach server-provided topics (avoid overwriting local-only drafts)
        for (const story of this.storiesForPrompt()) {
          // only hydrate stories that are persisted on the server (GUID id)
          if (!story.id || !this.isServerId(story.id)) continue;

          // Only attach subtopics that relate to this story's topicId (avoid attaching all topics to every story)
          if (story.topicId) {
            const rel = (topics || []).filter((t: any) => String(t.parentTopicId) === String(story.topicId) || String(t.topicId) === String(story.topicId));
            story.extraSubtopics = rel.map((t: any) => ({ topicId: t?.topicId, subtopic: t?.subtopic ?? '', notes: t?.notes ?? null }));
            // remove any extraSubtopics that reference deleted ids (defensive)
            if (Array.isArray(story.extraSubtopics)) {
              story.extraSubtopics = story.extraSubtopics.filter(es => !es.topicId || topicIds.has(String(es.topicId)) || !es.topicId);
            }
          } else {
            // Leave local-only stories' extraSubtopics intact — do not overwrite with global topic list
          }
          // prefill first three topic-subtopic slots only if not already set locally
          if (!story.reactionsSubtopic) story.reactionsSubtopic = topics[0]?.subtopic ?? story.reactionsSubtopic;
          if (!story.remixesSubtopic) story.remixesSubtopic = topics[1]?.subtopic ?? story.remixesSubtopic;
          if (!story.reactSubtopic) story.reactSubtopic = topics[2]?.subtopic ?? story.reactSubtopic;
        }

        // Ensure each parent topic from the server has a corresponding story for the current subject & prompt
        try {
          const parentTopics = (topics || []).filter((t: any) => !t?.parentTopicId);
          // expose parent topics for the template chip list
          this.loadedParentTopics = parentTopics ?? [];
          for (const parent of parentTopics) {
            const parentId = parent?.topicId ?? null;
            if (!parentId) continue;

            // If a story already exists with this topicId for this subject, skip
            const existsById = this.stories.some(s => s.topicId && String(s.topicId) === String(parentId) && (s.subjectId ?? null) === (this.currentSubjectId ?? null));
            if (existsById) continue;

            // Try to find a local story that matches this parent by name (created locally before server persistence)
            // Prefer stories that already belong to this subject, but also match stories that have no subject assigned yet
            const matchByNameIndex = this.stories.findIndex(s => {
              // skip stories that were just created locally (avoid accidental immediate matches)
              if ((s as any)._localCreatedAt && (Date.now() - (s as any)._localCreatedAt) < 5000) return false;
              return !s.topicId && (String(s.user || '').trim() === String(parent.name || '').trim() || String(s.user || '').trim() === String(parent.subtopic || '').trim()) && ((s.subjectId ?? null) === (this.currentSubjectId ?? null) || !s.subjectId);
            });
            // collect child subtopics (children of this parent)
            const children = (topics || []).filter((tt: any) => tt?.parentTopicId && String(tt.parentTopicId) === String(parentId));
            const extraSubtopics = (children.length > 0)
              ? children.map((c: any) => ({ topicId: c.topicId, subtopic: c.subtopic ?? '', notes: c.notes ?? null }))
              : [{ topicId: parent.topicId, subtopic: parent.subtopic ?? parent.name ?? '', notes: parent.notes ?? null }];

            if (matchByNameIndex >= 0) {
              // Attach the server topic id and server subtopics to the existing story
              const s = this.stories[matchByNameIndex];
              s.topicId = parentId;
              // associate this previously-local story with the current subject if it wasn't set
              s.subjectId = this.currentSubjectId ?? s.subjectId ?? null;
              s.extraSubtopics = extraSubtopics;
              // move matched story to the front for prominence
              this.stories.splice(matchByNameIndex, 1);
              this.stories = [s, ...this.stories];
              continue;
            }

            // No local match — create a new server-backed story for this parent topic
            const newStory: StoryCard = {
              id: parentId ?? this.generateLocalId(),
              promptId: this.selectedPromptId,
              subjectId: this.currentSubjectId ?? null,
              user: parent.name ?? parent.subtopic ?? 'Topic',
              timeAgo: 'just now',
              reactions: 0,
              remixes: 0,
              reactionsLabel: '0',
              remixesLabel: '0',
              reactLabel: 'React',
              remixLabel: 'Remix',
              extraSubtopics: extraSubtopics,
              topicId: parentId,
            };
            // prepend so server-backed topics show prominently
            this.stories = [newStory, ...this.stories];
          }
        } catch (err) {
          console.warn('Failed to hydrate stories from topics', err);
        }
        if (removed) {
          // persist changes if we removed stories
          this.saveStories();
        } else {
          this.saveStories();
        }

        // Also load server-backed stories for this subject and merge
        try {
          const serverStories = await firstValueFrom(this.api.getStories(this.schoolId!, this.currentSubjectId!));
          if (Array.isArray(serverStories)) {
            for (const ss of serverStories.reverse()) {
              // skip if already present
              const exists = this.stories.some(s => String(s.id) === String(ss.storyId) || (s.topicId && ss.topicId && String(s.topicId) === String(ss.topicId)));
              if (exists) continue;
              const newStory: StoryCard = {
                id: ss.storyId,
                promptId: ss.promptId ?? this.selectedPromptId,
                subjectId: ss.subjectId ?? this.currentSubjectId ?? null,
                user: ss.user ?? 'Story',
                timeAgo: 'just now',
                reactions: 0,
                remixes: 0,
                reactionsLabel: '0',
                remixesLabel: '0',
                reactLabel: 'React',
                remixLabel: 'Remix',
                extraSubtopics: [],
                topicId: ss.topicId ?? undefined,
              };
              this.stories = [newStory, ...this.stories];
            }
            this.saveStories();
          }
        } catch (err) {
          // ignore story load errors
        }
      },
      error: () => {
        // ignore and keep local state
      }
    });
  }
  private loadStories(): void {
    try {
      const raw = localStorage.getItem(this.STORIES_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as StoryCard[];
        if (Array.isArray(parsed)) {
          // ensure arrays for extraSubtopics exist to satisfy template strict checks
          for (const s of parsed) {
            if (!s.extraSubtopics) s.extraSubtopics = [];
            else {
              // Backwards compatibility: convert string arrays into object arrays
              if (s.extraSubtopics.length && typeof (s.extraSubtopics[0] as any) === 'string') {
                s.extraSubtopics = (s.extraSubtopics as unknown as string[]).map(x => ({ subtopic: x }));
              }
            }
          }
          this.stories = parsed;
        }
      }
    } catch (e) {
      console.warn('Unable to load stories from localStorage', e);
    }
  }

  private scrollSubtopicsStrip(): void {
    try {
      const el = this.subtopicStrip?.nativeElement;
      if (el) {
        // scroll to the far right so the newly-added subtopic is visible
        el.scrollTo({ left: el.scrollWidth, behavior: 'smooth' });
      }
    } catch (e) {
      // ignore scrolling errors
    }
  }
}

