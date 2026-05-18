import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText, IonSelect, IonSelectOption } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreatePersonRequest, StageDetails } from '../../services/api.service';

@Component({
  selector: 'app-create-teacher',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText, IonSelect, IonSelectOption],
  templateUrl: './create-teacher.page.html',
  styleUrls: ['./create-teacher.page.scss']
})
export class CreateTeacherPage implements OnInit {
  busy = false; message: string | null = null; error: string | null = null;
  stages: StageDetails[] = [];
  sublevels: Array<{ subLevelId: string; name: string; stageId: string }> = [];
  form = this.fb.group({ username: ['', Validators.required], password: ['', [Validators.required, Validators.minLength(8)]], firstName: ['', Validators.required], lastName: ['', Validators.required], stage: ['', Validators.required], subLevelId: [null], addSubject: [false], subjectName: [''], subjectDescription: [''] });
  constructor(private fb: FormBuilder, private api: ApiService, public router: Router) {}

  ngOnInit(): void {
    void this.loadStages();
    window.addEventListener('stages:created', this._stageListener = () => void this.loadStages());
  }

  private async loadStages(): Promise<void> {
    try {
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) return;
      const country = (this.api.loadAuth() as any)?.country ?? '';
      // if Kenya, prefer Kenyan CBC mapping
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
      // no global sublevel selector here — sublevels are loaded per-stage
    } catch (err) {
      console.warn('Failed to load stages', err);
    }
  }

  private _stageListener: any;

  async onStageChanged(): Promise<void> {
    this.sublevels = [];
    try { this.form.patchValue({ subLevelId: null }); } catch {}
    try {
      const raw = this.form.value as any;
      const schoolId = this.api.loadAuth()?.schoolId;
      const stageVal = raw.stage;
      if (!schoolId || !stageVal) return;
      if (typeof stageVal === 'string' && stageVal.length > 5) {
        const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageVal));
        this.sublevels = (subs || []).map(s => ({ subLevelId: s.subLevelId, name: s.name, stageId: stageVal }));
      }
    } catch (err) { console.warn('Failed to load sublevels', err); this.sublevels = []; }
  }

  // global sublevel selection removed; sublevels are selected per-stage only

  ngOnDestroy(): void {
    try { window.removeEventListener('stages:created', this._stageListener); } catch {}
  }
  async save(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const payload = this.form.value as CreatePersonRequest;
      // attach stageId if provided (backend will accept StageId/SubLevelId)
      const stage = this.form.get('stage')?.value;
      if (stage) (payload as any).stageId = stage;
      const subLevelId = this.form.get('subLevelId')?.value;
      if (subLevelId) (payload as any).subLevelId = subLevelId;
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) throw new Error('No school selected');
      const res = await firstValueFrom(this.api.createTeacher(schoolId, payload));
      // Optionally create a subject and assign to the new teacher
      const addSubject = this.form.get('addSubject')?.value;
      if (addSubject) {
        const sName = (this.form.get('subjectName')?.value || '').toString().trim();
        if (!sName) throw new Error('Subject name is required when adding a subject');
        const sDesc = (this.form.get('subjectDescription')?.value || '').toString().trim();
        const subjectPayload: any = { name: sName, stage: stage || payload.stage };
        if (sDesc) subjectPayload.description = sDesc;
        if (subLevelId) subjectPayload.subLevelId = subLevelId;
        const created = await firstValueFrom(this.api.createSubject(schoolId, subjectPayload));
        // assign created subject to teacher
        await firstValueFrom(this.api.assignTeacherSubject(schoolId, { teacherId: res.userId, subjectId: created.subjectId, subLevelId: subLevelId || undefined }));
      }

      this.message = `Teacher created: ${res.username}`;
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 600);
    } catch (err: any) { this.error = err instanceof Error ? err.message : 'Request failed'; }
    finally { this.busy = false; }
  }
}
