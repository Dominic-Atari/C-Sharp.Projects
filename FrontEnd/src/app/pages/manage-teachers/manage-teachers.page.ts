import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ModalController, PopoverController, IonButton, AlertController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';
import { CreateTeacherPage } from '../create-teacher/create-teacher.page';
import { TeacherDetailComponent } from '../dashboard/teacher-detail.component';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-manage-teachers',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './manage-teachers.page.html',
  styleUrls: ['./manage-teachers.page.scss']
})
export class ManageTeachersPage implements OnInit {
  teachers: Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }> = [];
  busy = false; error: string | null = null;
  // selection + details
  selectedTeacher?: { userId: string; username: string; firstName?: string | null; lastName?: string | null } | null = null;
  teacherDetailsLoading = false;
  teacherDetails: { schoolId?: string | null; schoolName?: string | null; subjects?: Array<any> } | null = null;

  constructor(
    private api: ApiService,
    private modalCtrl: ModalController,
    private popoverCtrl: PopoverController,
    private toastCtrl: ToastController
  ) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try { this.teachers = (await firstValueFrom(this.api.getTeachers((this.api.loadAuth() as any)?.schoolId))).map(t => ({ ...t })); } catch (err) { this.teachers = []; }
  }

  async addTeacher(): Promise<void> {
    const schoolId = (this.api.loadAuth() as any)?.schoolId;
    if (!schoolId) return;
    const p = await this.modalCtrl.create({ component: CreateTeacherPage });
    await p.present();
    const r = await p.onWillDismiss();
    if (r && r.data) {
      // reload list
      await this.load();
    }
  }

  async selectTeacher(t: any): Promise<void> {
    // open teacher detail popover and react to dismissal (refresh list on delete)
    const display = t.firstName ? (t.firstName + (t.lastName ? (' ' + t.lastName) : '')) : t.username;
    const p = await this.popoverCtrl.create({ component: TeacherDetailComponent as any, componentProps: { teacherId: t.userId, teacherName: display, teacherUsername: t.username, schoolName: (this.api.loadAuth() as any)?.schoolName ?? null }, translucent: true });
    await p.present();
    try {
      const r = await p.onDidDismiss();
      if (r && (r as any).data && (r as any).data.deleted) {
        await this.load();
        // clear selection if deleted teacher was selected
        if (this.selectedTeacher && this.selectedTeacher.userId === (r as any).data.teacherId) {
          this.selectedTeacher = null; this.teacherDetails = null;
        }
        const ttoast = await this.toastCtrl.create({ message: 'Teacher deleted', duration: 1500, color: 'success' });
        await ttoast.present();
      }
    } catch (e) { /* ignore */ }
  }

  onSelectedTeacherChange(userId: string | null | undefined): void {
    if (!userId) {
      this.selectedTeacher = null;
      this.teacherDetails = null;
      return;
    }
    const found = this.teachers.find(x => x.userId === userId) ?? null;
    if (found) this.selectTeacher(found);
  }

    private async loadTeacherDetails(userId: string): Promise<void> {
      this.teacherDetailsLoading = true;
      this.teacherDetails = { subjects: [] };
      try {
        const schoolId = (this.api.loadAuth() as any)?.schoolId;
        this.teacherDetails!.schoolId = schoolId ?? null;
        this.teacherDetails!.schoolName = (this.api.loadAuth() as any)?.schoolName ?? null;

        // fetch assigned subjects
        const subs = await firstValueFrom(this.api.getTeacherSubjects(schoolId, userId));
        const arr = Array.isArray(subs) ? subs : [];
        // for each subject fetch topics
        const subjectsWithTopics = await Promise.all(arr.map(async (s: any) => {
          const subjectId = s.subjectId;
          let topics: any[] = [];
          try { topics = await firstValueFrom(this.api.getTopics(schoolId, subjectId)); } catch {}
          return { subjectId, name: s.name ?? '', topics: Array.isArray(topics) ? topics : [] };
        }));

        this.teacherDetails!.subjects = subjectsWithTopics;
      } catch (err) {
        // ignore but show basic error state
        this.teacherDetails = { subjects: [] };
      } finally {
        this.teacherDetailsLoading = false;
      }
    }

    // Teacher deletion is handled via the popover. The popover dismiss handler refreshes the list.
}
