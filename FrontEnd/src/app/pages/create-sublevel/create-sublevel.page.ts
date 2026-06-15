import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText, IonSelect, IonSelectOption, ModalController, ToastController } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreateSubLevelRequest, StageDetails, SubLevelDetails } from '../../services/api.service';

@Component({
  selector: 'app-create-sublevel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText, IonSelect, IonSelectOption],
  templateUrl: './create-sublevel.page.html',
  styleUrls: ['./create-sublevel.page.scss']
})
export class CreateSubLevelPage implements OnInit {
  busy = false; message: string | null = null; error: string | null = null;
  stages: StageDetails[] = [];
  existingSublevels: SubLevelDetails[] = [];

  form = this.fb.group({ stageId: ['', Validators.required], name: ['', Validators.required], label: [''], description: [''] });

  constructor(private fb: FormBuilder, private api: ApiService, private modal: ModalController, private toastCtrl: ToastController) {}

  ngOnInit(): void { void this.loadStages(); }

  async loadStages(): Promise<void> {
    try {
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) return;
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
    } catch (err) { console.warn('Failed to load stages', err); this.stages = []; }
  }

  async onStageChanged(): Promise<void> {
    this.existingSublevels = [];
    try {
      const raw = this.form.value as any;
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) return;
      const stageVal = raw.stageId;
      if (!stageVal) return;
      const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageVal));
      this.existingSublevels = subs ?? [];
      try { this.form.patchValue({ name: '' }); } catch {}
    } catch (err) { console.warn('Failed to load sublevels for stage', err); this.existingSublevels = []; }
  }

  isDuplicateName(): boolean {
    const name = (this.form.get('name')?.value || '').trim().toLowerCase();
    if (!name) return false;
    return this.existingSublevels.some(s => (s.name || '').toLowerCase() === name);
  }

  get existingSublevelNames(): string {
    return (this.existingSublevels || []).map(s => s.name).filter(n => !!n).join(', ');
  }

  async save(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.form.value as any;
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) throw new Error('No school selected');
      if (!raw.stageId) throw new Error('Stage is required');
      // prevent duplicate names within stage
      const nameLower = (raw.name || '').trim().toLowerCase();
      if (this.existingSublevels.some(s => (s.name || '').toLowerCase() === nameLower)) { this.error = 'A sublevel with this name already exists for the selected stage.'; return; }
      const payload: CreateSubLevelRequest = { name: (raw.name || '').trim(), label: (raw.label || '').trim() || null, description: (raw.description || '').trim() || null };
      const res = await firstValueFrom(this.api.createSubLevel(schoolId, raw.stageId, payload));
      this.message = 'Sublevel created';
      try { window.dispatchEvent(new CustomEvent('stages:created', { detail: { schoolId } })); } catch {}
      const t = await this.toastCtrl.create({ message: 'Sublevel created', duration: 1400, color: 'success' }); await t.present();
      await this.modal.dismiss(res);
    } catch (err: any) { this.error = err instanceof Error ? err.message : 'Failed to create sublevel'; }
    finally { this.busy = false; }
  }

  cancel(): void { this.modal.dismiss(); }
}
