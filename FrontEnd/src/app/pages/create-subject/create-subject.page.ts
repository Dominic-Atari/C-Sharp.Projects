import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonSelect, IonSelectOption, IonText } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreateSubjectRequest, StageDetails } from '../../services/api.service';

@Component({
  selector: 'app-create-subject',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonSelect, IonSelectOption, IonText],
  templateUrl: './create-subject.page.html',
  styleUrls: ['./create-subject.page.scss']
})
export class CreateSubjectPage {
  busy = false;
  message: string | null = null;
  error: string | null = null;

  stages: StageDetails[] = [];
  sublevels: Array<{ subLevelId: string; name: string; stageId: string }> = [];

  form = this.fb.group({ name: ['', Validators.required], stage: ['', Validators.required], subLevelId: [null], description: [''] });

  ngOnInit(): void {
    void this.loadStages();
    // listen for external stage creations and reload
    window.addEventListener('stages:created', this._stageListener = () => void this.loadStages());
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
      console.warn('Failed to load stages', err);
    }
  }

  async onStageChanged(): Promise<void> {
    this.sublevels = [];
    try {
      const raw = this.form.value as any;
      const schoolId = this.api.loadAuth()?.schoolId;
      const stageVal = raw.stage;
      if (!schoolId || !stageVal) return;
      // If stage is a GUID (school-specific), load its sublevels
      if (typeof stageVal === 'string' && stageVal.length > 5) {
        const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageVal));
        this.sublevels = (subs || []).map(s => ({ subLevelId: s.subLevelId, name: s.name, stageId: stageVal }));
        // If there are sublevels for this stage, make subLevelId required
        if ((this.sublevels || []).length > 0) {
          this.form.get('subLevelId')?.setValidators([Validators.required]);
        } else {
          this.form.get('subLevelId')?.clearValidators();
        }
        this.form.get('subLevelId')?.updateValueAndValidity();
      }
    } catch (err) { console.warn('Failed to load sublevels', err); this.sublevels = []; }
  }

  get needsSublevelRequired(): boolean {
    return (this.sublevels || []).length > 0 && !!this.form.get('stage')?.value;
  }

  private _stageListener: any;

  ngOnDestroy(): void {
    try { window.removeEventListener('stages:created', this._stageListener); } catch {}
  }

  constructor(private fb: FormBuilder, private api: ApiService, public router: Router) {}

  async save(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.form.value as any;
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) throw new Error('No school selected');

      // Normalize payload: backend accepts either numeric `stage` (enum) OR `stageId` (GUID)
      const stageVal = raw.stage;
      let payload: any = { name: (raw.name || '').trim(), description: raw.description?.trim() };
      if (stageVal !== undefined && stageVal !== null && stageVal !== '') {
        if (typeof stageVal === 'number') {
          payload.stage = stageVal;
        } else {
          // If it's a numeric string, send as number; otherwise send as stageId string
          const maybeNumber = Number(stageVal);
          if (!Number.isNaN(maybeNumber) && String(stageVal).trim() !== '') {
            payload.stage = maybeNumber;
          } else {
            payload.stageId = String(stageVal).trim();
          }
        }
      }

      // include optional sublevel selection
      if (raw.subLevelId) payload.subLevelId = raw.subLevelId;
      // enforce sublevel when available
      if (this.needsSublevelRequired && !raw.subLevelId) throw new Error('Please select a Sublevel for the chosen Stage');

      const res = await firstValueFrom(this.api.createSubject(schoolId, payload));
      this.message = `Subject created: ${res.subjectId}`;
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 600);
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }
}
