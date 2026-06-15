import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, PopoverController, AlertController, ToastController } from '@ionic/angular';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-teacher-detail',
  standalone: true,
  imports: [CommonModule, IonicModule],
  template: `
  <ion-card>
    <ion-card-header>
      <ion-card-title>{{ teacherName }}</ion-card-title>
    </ion-card-header>
    <ion-card-content>
      <p *ngIf="!subjects?.length">No subjects assigned.</p>
      <ion-list *ngIf="subjects?.length">
        <ion-item *ngFor="let s of subjects">
          <ion-label>
            <h3>{{ s.name }}</h3>
            <p class="muted">Stage: {{ s.stage }}</p>
            <p *ngIf="s.description">{{ s.description }}</p>
          </ion-label>
        </ion-item>
      </ion-list>
      <div style="margin-bottom:8px">
        <p *ngIf="teacherUsername"><strong>Username:</strong> {{ teacherUsername }}</p>
        <p *ngIf="schoolName"><strong>School:</strong> {{ schoolName }}</p>
        <p *ngIf="stageName"><strong>Stage:</strong> {{ stageName }}</p>
        <p *ngIf="!stageName && stageId"><strong>Stage ID:</strong> {{ stageId }}</p>
        <p *ngIf="subLevelName"><strong>Sublevel:</strong> {{ subLevelName }}</p>
        <p *ngIf="!subLevelName && subLevelId"><strong>Sublevel ID:</strong> {{ subLevelId }}</p>
      </div>
      <div style="margin-top:12px;display:flex;gap:8px">
        <ion-button expand="block" color="danger" (click)="confirmDelete()" [disabled]="busy">Delete</ion-button>
        <ion-button expand="block" (click)="close()">Close</ion-button>
      </div>
    </ion-card-content>
  </ion-card>
  `,
})
export class TeacherDetailComponent {
  @Input() teacherId!: string;
  @Input() teacherName!: string;
  @Input() teacherUsername?: string;
  @Input() schoolName?: string | null;
  subjects: Array<{ subjectId: string; name: string; stage: number; description?: string | null }> = [];
  stageId?: string | null = null;
  stageName?: string | null = null;
  subLevelId?: string | null = null;
  subLevelName?: string | null = null;
  busy = false;

  constructor(private api: ApiService, private pop: PopoverController, private alertCtrl: AlertController, private toastCtrl: ToastController) {}

  async confirmDelete(): Promise<void> {
    const alert = await this.alertCtrl.create({
      header: 'Confirm delete',
      message: `Delete ${this.teacherName}? This cannot be undone.`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Delete', role: 'destructive', handler: () => void this.deleteTeacher() }
      ]
    });
    await alert.present();
  }

  private async deleteTeacher(): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      const auth = this.api.loadAuth();
      if (!auth?.schoolId) throw new Error('No school selected');
      const res = await firstValueFrom(this.api.deleteTeacher(auth.schoolId, this.teacherId));
      if (!res || !res.isDeleted) throw new Error('Delete API did not confirm deletion');
      // dismiss popover and signal deletion
      await this.pop.dismiss({ deleted: true, teacherId: this.teacherId });
      const t = await this.toastCtrl.create({ message: 'Teacher deleted', duration: 1400, color: 'success' });
      await t.present();
    } catch (err: any) {
        const msg = err?.message ?? 'Failed to delete';
        const t = await this.toastCtrl.create({ message: msg, duration: 3000, color: 'danger' });
        await t.present();
        try {
          const alert = await this.alertCtrl.create({ header: 'Delete failed', message: msg, buttons: ['OK'] });
          await alert.present();
        } catch { /* ignore */ }
    } finally {
      this.busy = false;
    }
  }

  async ngOnInit(): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      if (!auth?.schoolId || !this.teacherId) return;
      this.subjects = await firstValueFrom(this.api.getTeacherSubjects(auth.schoolId, this.teacherId));

      // load membership to determine assigned stage/sublevel
      try {
        const membership = await firstValueFrom(this.api.getMembership(auth.schoolId, this.teacherId));
        this.stageId = membership.stageId ?? null;
        this.subLevelId = membership.subLevelId ?? null;

        if (this.stageId) {
          try {
            const stages = await firstValueFrom(this.api.getStages(auth.schoolId));
            const st = (stages || []).find(s => s.stageId === this.stageId);
            this.stageName = st?.name ?? null;
          } catch (e) { /* ignore */ }
        }

        if (this.stageId && this.subLevelId) {
          try {
            const subs = await firstValueFrom(this.api.getSubLevels(auth.schoolId, this.stageId));
            const sl = (subs || []).find(s => s.subLevelId === this.subLevelId);
            this.subLevelName = sl?.name ?? null;
          } catch (e) { /* ignore */ }
        }
      } catch (e) {
        // ignore membership load errors
      }
    } catch (err) {
      console.warn('Failed to load teacher subjects', err);
    }
  }

  close(): void {
    this.pop.dismiss();
  }
}
