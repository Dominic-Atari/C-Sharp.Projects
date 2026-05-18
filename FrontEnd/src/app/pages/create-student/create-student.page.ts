import { Component, OnInit } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonButton, IonCard, IonCardContent, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonTitle, IonToolbar, IonSelect, IonSelectOption } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreatePersonRequest, StageDetails } from '../../services/api.service';

@Component({
    selector: 'app-create-student',
    imports: [ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonSelect, IonSelectOption],
    templateUrl: './create-student.page.html',
    styleUrls: ['./create-student.page.scss']
})
export class CreateStudentPage implements OnInit {
  stages: StageDetails[] = [];
  sublevels: Array<{ subLevelId: string; name: string; stageId: string }> = [];
  studentForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    stage: ['', Validators.required],
    subLevelId: [null]
  });
  busy = false;
  message: string | null = null;
  error: string | null = null;

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    void this.loadStages();
    window.addEventListener('stages:created', this._stageListener = () => void this.loadStages());
  }

  async onStageChanged(): Promise<void> {
    this.sublevels = [];
    try {
      this.studentForm.patchValue({ subLevelId: null });
    } catch {}
    try {
      const raw = this.studentForm.value as any;
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) return;
      const stageVal = raw.stage;
      if (!stageVal) return;
      if (typeof stageVal === 'string' && stageVal.length > 5) {
        const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageVal));
        this.sublevels = (subs || []).map(s => ({ subLevelId: s.subLevelId, name: s.name, stageId: stageVal }));
      }
    } catch (err) { console.warn('Failed to load sublevels for stage', err); this.sublevels = []; }
  }

  private async loadStages(): Promise<void> {
    try {
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) return;
      const country = (this.api.loadAuth() as any)?.country ?? '';
      if ((country || '').toLowerCase().includes('kenya')) {
        this.stages = [
          { stageId: 'EYE', name: 'Early Years Education (PP1, PP2, Grades 1-3)' },
          { stageId: 'UPPER_PRIMARY', name: 'Upper Primary (Grades 4-6)' },
          { stageId: 'JUNIOR_SECONDARY', name: 'Junior Secondary (Grades 7-9)' },
          { stageId: 'SENIOR_SECONDARY', name: 'Senior Secondary (Grades 10-12)' },
        ];
        return;
      }
      this.stages = await firstValueFrom(this.api.getStages(schoolId));
    } catch (err) {
      console.warn('Failed load stages', err);
    }
  }

  private _stageListener: any;

  ngOnDestroy(): void {
    try { window.removeEventListener('stages:created', this._stageListener); } catch {}
  }

  async createStudent(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) throw new Error('Missing schoolId');
      const payload = this.studentForm.value as CreatePersonRequest;
      const stage = this.studentForm.get('stage')?.value;
      if (stage) (payload as any).stageId = stage;
      const subLevelId = this.studentForm.get('subLevelId')?.value;
      if (subLevelId) (payload as any).subLevelId = subLevelId;
      try { console.log('CreateStudent payload', JSON.stringify(payload)); } catch {}
      const res = await firstValueFrom(this.api.createStudent(schoolId, payload));
      this.message = `Student created: ${res.username}`;
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 700);
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }
}
