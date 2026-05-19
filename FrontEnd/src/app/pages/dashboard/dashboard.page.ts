import { Component, AfterViewInit, OnDestroy } from '@angular/core';

import { IonicModule } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { personAddOutline, peopleOutline } from 'ionicons/icons';
import { Router } from '@angular/router';
import { ActionSheetController, PopoverController, ModalController } from '@ionic/angular';
import { SchoolPopoverComponent } from '../dashboard/school-popover.component';
import { firstValueFrom } from 'rxjs';
import { ApiService, StageDetails } from '../../services/api.service';

@Component({
    selector: 'app-dashboard',
    imports: [
    IonicModule
],
    templateUrl: './head-teachers-page.html',
    styleUrls: ['./dashboard.page.scss']
})
export class DashboardPage implements AfterViewInit, OnDestroy {
  auth: any = null;
  teachersCount = 0;
  studentsCount = 0;
  stages: StageDetails[] = [];
  levelCounts: Array<{ stageId: string; name: string; count: number }> = [];
  private _studentsUpdatedListener: any;
  private _stagesCreatedListener: any;

  constructor(private api: ApiService, private router: Router, private actionSheetCtrl: ActionSheetController, private popoverCtrl: PopoverController, private modalCtrl: ModalController) {
    addIcons({ peopleOutline, personAddOutline });
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

  async ngAfterViewInit(): Promise<void> {
    this.auth = this.api.loadAuth();
    const schoolId = this.auth?.schoolId;
    if (schoolId) {
      try {
        const teachers = await firstValueFrom(this.api.getTeachers(schoolId));
        this.teachersCount = Array.isArray(teachers) ? teachers.length : 0;
        const students = await firstValueFrom(this.api.getStudents(schoolId));
        this.studentsCount = Array.isArray(students) ? students.length : 0;
        await this.loadLevelsAndCounts(schoolId, students as any[]);
        this._studentsUpdatedListener = (_ev: any) => { void this.loadLevelsAndCounts(schoolId); };
        this._stagesCreatedListener = (_ev: any) => { void this.loadLevelsAndCounts(schoolId); };
        try { window.addEventListener('students:updated', this._studentsUpdatedListener); } catch {}
        try { window.addEventListener('stages:created', this._stagesCreatedListener); } catch {}
      } catch (err) {
        console.warn('Failed to load dashboard counts', err);
      }
    }
  }

  get headDisplayName(): string {
    return this.auth?.username ?? '';
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
      await this.router.navigateByUrl(`/stages/${stageId}/performance`);
    } catch (err) {
      console.warn('Failed to open level performance', err);
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

  openManageLevels(): void {
    this.router.navigateByUrl('/manage/levels');
  }

  openLevelSublevels(stageId: string): void {
    if (!stageId) return;
    void this.router.navigateByUrl(`/stages/${stageId}/sublevels`);
  }
}
