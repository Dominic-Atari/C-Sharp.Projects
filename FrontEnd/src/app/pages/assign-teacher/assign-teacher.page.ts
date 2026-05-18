import { Component } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonButton, IonCard, IonCardContent, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonSelect, IonSelectOption, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-assign-teacher',
    imports: [ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonSelect, IonSelectOption, IonButton],
    templateUrl: './assign-teacher.page.html',
    styleUrls: ['./assign-teacher.page.scss']
})
export class AssignTeacherPage {
  assignForm = this.fb.group({ teacherId: ['', Validators.required], subjectId: ['', Validators.required], stageId: [null], subLevelId: [null] });
  busy = false; message: string | null = null; error: string | null = null;
  teachers: Array<any> = [];
  subjects: Array<any> = [];
  allSubjects: Array<any> = [];
  subjectsFiltered: Array<any> = [];
  stages: Array<{ stageId: string; name: string; label?: string | null }> = [];
  subLevels: Record<string, Array<{ subLevelId: string; name: string }>> = {};
  allSublevels: Array<{ subLevelId: string; name: string; stageId: string; stageName?: string }> = [];

  teachersLoading = false; teachersError: string | null = null;
  subjectsLoading = false; subjectsError: string | null = null;

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {}

  async ngOnInit(): Promise<void> {
    const schoolId = this.api.loadAuth()?.schoolId;
    if (!schoolId) {
      this.teachersError = this.subjectsError = 'Missing schoolId';
      return;
    }

    // Load teachers and subjects independently so a failure in one doesn't hide the other
    this.teachersLoading = true;
    try {
      this.teachers = await firstValueFrom(this.api.getTeachers(schoolId));
    } catch (err:any) {
      console.warn('Failed to load teachers', err);
      this.teachersError = err instanceof Error ? err.message : 'Failed to load teachers';
      this.teachers = [];
    } finally { this.teachersLoading = false; }

    this.subjectsLoading = true;
    try {
      this.subjects = await firstValueFrom(this.api.getSubjects(schoolId));
      this.allSubjects = this.subjects || [];
      this.subjectsFiltered = [...this.allSubjects];
    } catch (err:any) {
      console.warn('Failed to load subjects', err);
      this.subjectsError = err instanceof Error ? err.message : 'Failed to load subjects';
      this.subjects = [];
      this.allSubjects = [];
      this.subjectsFiltered = [];
    } finally { this.subjectsLoading = false; }

    // load stages and preload all sublevels
    try {
      this.stages = await firstValueFrom(this.api.getStages(schoolId));
      (async () => {
        try {
          const all: Array<{ subLevelId: string; name: string; stageId: string; stageName?: string }> = [];
          for (const st of this.stages) {
            try {
              const subs = await firstValueFrom(this.api.getSubLevels(schoolId, st.stageId));
              for (const sl of (subs || [])) all.push({ subLevelId: sl.subLevelId, name: sl.name, stageId: st.stageId, stageName: st.name });
            } catch (e) { console.debug('Failed to load sublevels for stage', st.stageId, e); }
          }
          this.allSublevels = all;
        } catch (err) { this.allSublevels = []; }
      })();
    } catch (e) { this.stages = []; }
  }

  onAllSublevelSelected(subLevelId: string | null): void {
    if (!subLevelId) return;
    const sl = this.allSublevels.find(s => s.subLevelId === subLevelId);
    if (!sl) return;
    try { this.assignForm.get('stageId')?.setValue(sl.stageId as any); } catch {}
    try { this.assignForm.get('subLevelId')?.setValue(sl.subLevelId as any); } catch {}
    // clear subject selection when changing scope
    try { this.assignForm.get('subjectId')?.setValue(null as any); } catch {}
    void this.onStageSelected(sl.stageId);
  }

  async assign(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) throw new Error('Missing schoolId');
      const v = this.assignForm.value as { teacherId?: string | null; subjectId?: string | null };
      if (!v.teacherId || !v.subjectId) throw new Error('Please select both a teacher and a subject');
      const payload: any = { teacherId: v.teacherId, subjectId: v.subjectId } as { teacherId: string; subjectId: string };
      // include optional scope fields
      const stageId = (this.assignForm.value as any).stageId;
      const subLevelId = (this.assignForm.value as any).subLevelId;
      if (subLevelId) payload.subLevelId = subLevelId;
      const res = await firstValueFrom(this.api.assignTeacherSubject(schoolId, payload));
      this.message = `Assigned ${res.teacherId} -> ${res.subjectId}`;
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 700);
    } catch (err:any) { this.error = err instanceof Error ? err.message : 'Failed'; }
    finally { this.busy = false; }
  }

  async onStageSelected(stageId: string | null): Promise<void> {
    if (!stageId) return;
    const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) return;
    try {
      const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageId));
      this.subLevels[stageId] = subs ?? [];
      // Filter subjects to this stage
      this.filterSubjects(stageId, null);
      // clear subject selection when changing scope
      try { this.assignForm.get('subjectId')?.setValue(null as any); } catch {}
    } catch (err) { this.subLevels[stageId] = []; }
  }

  onSublevelSelected(subLevelId: string | null): void {
    const stageId = this.assignForm.get('stageId')?.value ?? null;
    this.filterSubjects(stageId, subLevelId ?? null);
    try { this.assignForm.get('subjectId')?.setValue(null as any); } catch {}
  }

  private stageNameToNumeric(name?: string | null): number | null {
    if (!name) return null;
    const n = name.replace(/\s+/g, '').toLowerCase();
    const map: Record<string, number> = {
      preschool: 1,
      primary: 2,
      middleschool: 3,
      highschool: 4,
      undergraduate: 5,
      graduate: 6,
      professional: 7
    };
    return map[n] ?? null;
  }

  private filterSubjects(stageId?: string | null, subLevelId?: string | null): void {
    // If no filters, show all
    if (!stageId && !subLevelId) { this.subjectsFiltered = [...this.allSubjects]; return; }
    let stageNumeric: number | null = null;
    if (stageId) {
      const st = this.stages.find(s => s.stageId === stageId);
      stageNumeric = this.stageNameToNumeric(st?.label ?? st?.name ?? null);
    }
    this.subjectsFiltered = (this.allSubjects || []).filter(s => {
      const byStage = stageNumeric == null ? true : Number(s.stage) === stageNumeric;
      const bySub = subLevelId ? ((s.subLevelId ?? null) === subLevelId) : true;
      return byStage && bySub;
    });
  }

  get selectedStageId(): string | null {
    return (this.assignForm.value as any).stageId ?? null;
  }
}
