import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline, logOutOutline, personAddOutline, peopleOutline } from 'ionicons/icons';
import { Router } from '@angular/router';
import { ActionSheetController, PopoverController, ModalController } from '@ionic/angular';
import { SchoolPopoverComponent } from '../dashboard/school-popover.component';
import { firstValueFrom } from 'rxjs';
import { ApiService, StageDetails } from '../../services/api.service';

interface PromptRoom {
  id: string;
  title: string;
  category: string;
  timeLeft: string;
  streakDays: number;
  participants: number;
  mood: 'violet' | 'teal' | 'amber' | 'pink';
}

interface StoryCard {
  id: string;
  promptId: string;
  user: string;
  timeAgo: string;
  reactions: number;
  remixes: number;
  badge?: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    IonicModule,
  ],
    templateUrl: './head-teachers-page.html',
    // Renamed dashboard template for head teachers
    // Note: file renamed to `head-teachers-page.html`
  styleUrls: ['./dashboard.page.scss'],
})
export class DashboardPage implements OnInit {
  prompts: PromptRoom[] = [
    { id: 'p1', title: 'Show your 10-sec win', category: 'Momentum', timeLeft: '12h left', streakDays: 4, participants: 182, mood: 'violet' },
    { id: 'p2', title: 'Weekend plans in 15s', category: 'Social', timeLeft: '8h left', streakDays: 2, participants: 96, mood: 'teal' },
    { id: 'p3', title: 'Desk setup snapshot', category: 'Work', timeLeft: '18h left', streakDays: 1, participants: 143, mood: 'amber' },
  ];

  stories: StoryCard[] = [
    { id: 's1', promptId: 'p1', user: 'Amina', timeAgo: '2h ago', reactions: 64, remixes: 7, badge: 'Remix-ready' },
    { id: 's2', promptId: 'p1', user: 'Luis', timeAgo: '3h ago', reactions: 48, remixes: 3 },
    { id: 's3', promptId: 'p2', user: 'Priya', timeAgo: '1h ago', reactions: 92, remixes: 11, badge: 'Trending' },
    { id: 's4', promptId: 'p3', user: 'Noah', timeAgo: '30m ago', reactions: 21, remixes: 2 },
    { id: 's5', promptId: 'p2', user: 'Maya', timeAgo: '4h ago', reactions: 35, remixes: 5 },
    { id: 's6', promptId: 'p1', user: 'Zoe', timeAgo: '10m ago', reactions: 18, remixes: 1 },
  ];

  selectedPromptId = this.prompts[0].id;
  auth: any = null;
  teachersCount = 0;
  studentsCount = 0;
  stages: StageDetails[] = [];
  levelCounts: Array<{ stageId: string; name: string; count: number }> = [];
  private _studentsUpdatedListener: any;
  private _stagesCreatedListener: any;

  constructor(private api: ApiService, private router: Router, private actionSheetCtrl: ActionSheetController, private popoverCtrl: PopoverController, private modalCtrl: ModalController) {
    addIcons({peopleOutline,addCircleOutline,heartOutline,repeatOutline,logOutOutline,flameOutline,timeOutline,playOutline,personAddOutline});
  }

  async openSchoolPopover(ev: Event): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      const p = await this.popoverCtrl.create({
        component: SchoolPopoverComponent,
        event: ev,
        translucent: true,
        componentProps: { schoolId: auth?.schoolId, schoolName: auth?.schoolName, county: auth?.county }
      });
      await p.present();
      await p.onWillDismiss();
    } catch (err) {
      console.warn('failed to present school popover', err);
    }
  }

  ngOnInit(): void {}

  async ngAfterViewInit(): Promise<void> {
    // load auth and teacher count for the dashboard header
    this.auth = this.api.loadAuth();
    const schoolId = this.auth?.schoolId;
    if (schoolId) {
      try {
        const teachers = await firstValueFrom(this.api.getTeachers(schoolId));
        this.teachersCount = Array.isArray(teachers) ? teachers.length : 0;
        const students = await firstValueFrom(this.api.getStudents(schoolId));
        this.studentsCount = Array.isArray(students) ? students.length : 0;
        // load stages and compute counts per level
        await this.loadLevelsAndCounts(schoolId, students as any[]);
        // listen for updates from other pages
        this._studentsUpdatedListener = (_ev: any) => { void this.loadLevelsAndCounts(schoolId); };
        this._stagesCreatedListener = (_ev: any) => { void this.loadLevelsAndCounts(schoolId); };
        try { window.addEventListener('students:updated', this._studentsUpdatedListener); } catch {}
        try { window.addEventListener('stages:created', this._stagesCreatedListener); } catch {}
      } catch (err) {
        console.warn('Failed to load teachers count', err);
      }
    }
  }

  get headDisplayName(): string {
    if (!this.auth) return '';
    const first = (this.auth as any).firstName || '';
    const last = (this.auth as any).lastName || '';
    const username = (this.auth as any).username || (this.auth as any).userId || '';
    const name = ((first || last) ? `${first} ${last}`.trim() : username);
    return name;
  }

  selectPrompt(id: string): void {
    this.selectedPromptId = id;
  }

  storiesForPrompt(): StoryCard[] {
    return this.stories.filter(s => s.promptId === this.selectedPromptId);
  }

  addStory(): void {
    const mock: StoryCard = {
      id: 's' + (this.stories.length + 1),
      promptId: this.selectedPromptId,
      user: 'You',
      timeAgo: 'just now',
      reactions: 0,
      remixes: 0,
      badge: 'New',
    };
    this.stories = [mock, ...this.stories];
  }

  async openTeacherActions(): Promise<void> {
    const sheet = await this.actionSheetCtrl.create({
      header: 'Teachers',
      buttons: [
        {
          text: `Add new teacher`,
          icon: 'person-add-outline',
          handler: () => void this.router.navigateByUrl('/create/teacher'),
        },
        {
          text: 'Assign teacher to subject',
          icon: 'people-outline',
          handler: () => void this.router.navigateByUrl('/assign/teacher-subject'),
        },
        { text: 'Cancel', role: 'cancel' },
      ],
    });
    await sheet.present();
  }

  async openStudentActions(): Promise<void> {
    const sheet = await this.actionSheetCtrl.create({
      header: 'Students',
      buttons: [
        {
          text: 'View performance',
          icon: 'bar-chart-outline',
          handler: () => void this.openStudentPerformance(),
        },
        {
          text: `Add new student`,
          icon: 'person-add-outline',
          handler: () => void this.router.navigateByUrl('/create/student'),
        },
        { text: 'Cancel', role: 'cancel' },
      ],
    });
    await sheet.present();
  }

  async openStudentPerformance(): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId;
      const students = schoolId ? await firstValueFrom(this.api.getStudents(schoolId)) : [];
      const modal = await this.modalCtrl.create({
        component: (await import('../../components/chat-performance/chat-performance.component')).ChatPerformanceComponent,
        componentProps: { schoolId: schoolId ?? null, students: students ?? [] },
        cssClass: 'performance-chat-modal'
      });
      await modal.present();
    } catch (err) {
      console.warn('Failed to open students performance', err);
    }
  }

  async openLevelPerformance(stageId: string): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId;
      if (!schoolId) return;
      // navigate to dedicated stage performance page instead of modal
      await this.router.navigateByUrl(`/stages/${stageId}/performance`);
    } catch (err) {
      console.warn('Failed to open level performance', err);
    }
  }

  async openLevelTopics(stageId: string, ev?: Event): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId;
      if (!schoolId) return;
      const stage = this.stages?.find(s => s.stageId === stageId);
      const modal = await this.modalCtrl.create({
        component: (await import('../../components/subject-topic-picker/subject-topic-picker.component')).SubjectTopicPickerComponent,
        componentProps: { schoolId, stageId, stageName: stage?.label ?? stage?.name ?? null },
        cssClass: 'subject-topic-picker-modal'
      });
      await modal.present();
    } catch (err) {
      console.warn('Failed to open topic picker', err);
    }
  }

  private async loadLevelsAndCounts(schoolId: string, students?: Array<any>): Promise<void> {
    try {
      this.stages = await firstValueFrom(this.api.getStages(schoolId));
      const stus = students ?? (await firstValueFrom(this.api.getStudents(schoolId)));
      const counts: Record<string, number> = {};
      for (const s of this.stages) counts[s.stageId] = 0;
      for (const s of stus ?? []) {
        const key = (s as any).stageId ?? null;
        if (key && counts[key] !== undefined) counts[key] += 1;
      }
      this.levelCounts = this.stages.map(s => ({ stageId: s.stageId, name: s.name, count: counts[s.stageId] ?? 0 }));
      // Dev debug: log what we received so we can trace empty responses
      try { console.debug('Dashboard: loaded stages', this.stages, 'students', (stus ?? []).length, 'levelCounts', this.levelCounts); } catch {}
      // expose raw data for quick inspection in dev builds
      (window as any).__debugLevels = { stages: this.stages, students: stus, levelCounts: this.levelCounts };
    } catch (err) {
      console.warn('Failed to load levels/counts', err);
      this.stages = [];
      this.levelCounts = [];
    }
  }

  ngOnDestroy(): void {
    try { window.removeEventListener('students:updated', this._studentsUpdatedListener); } catch {}
    try { window.removeEventListener('stages:created', this._stagesCreatedListener); } catch {}
  }

  goToEdit(): void {
    this.router.navigateByUrl('/school/edit');
  }

  openManageLevels(): void {
    this.router.navigateByUrl('/manage/levels');
  }

  async openManageLevelsModal(ev?: Event): Promise<void> {
    try {
      const buttons: any[] = (this.stages || []).map(s => ({ text: s.name, handler: () => { void this.router.navigateByUrl(`/stages/${s.stageId}/sublevels`); } }));
      buttons.push({ text: 'Cancel', role: 'cancel' as any });
      const sheet = await this.actionSheetCtrl.create({ header: 'Levels', buttons: buttons as any });
      await sheet.present();
    } catch (err) { console.warn('Failed to open manage levels modal', err); }
  }

  openLevelSublevels(stageId: string): void {
    if (!stageId) return;
    void this.router.navigateByUrl(`/stages/${stageId}/sublevels`);
  }

  logout(): void {
    this.api.clearAuth();
    this.router.navigateByUrl('/auth?mode=login');
  }
}
