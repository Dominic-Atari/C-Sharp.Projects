import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, AlertController, ToastController } from '@ionic/angular';
import { ApiService, StageDetails } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

interface Student { userId: string; username?: string | null; firstName?: string | null; lastName?: string | null; stageId?: string | null; level?: string | null }

@Component({
  selector: 'app-manage-students',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './manage-students.page.html',
  styleUrls: ['./manage-students.page.scss']
})
export class ManageStudentsPage implements OnInit {
  stages: StageDetails[] = [];
  students: Student[] = [];
  grouped: Record<string, Student[]> = {};
  selected = new Set<string>();
  loading = false;

  // expose keys for template iteration (templates cannot reference global Object)
  get groupedKeys(): string[] {
    try { return Object.keys(this.grouped); } catch { return []; }
  }

  stageName(key: string): string {
    if (key === 'unassigned') return 'Unassigned';
    const s = this.stages.find(st => st.stageId === key);
    return s?.name ?? key;
  }

  constructor(private api: ApiService, private alertCtrl: AlertController, private toastCtrl: ToastController) {}

  ngOnInit(): void { void this.loadAll(); }

  async loadAll(): Promise<void> {
    try {
      this.loading = true;
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) return;
      this.stages = await firstValueFrom(this.api.getStages(schoolId));
      this.students = await firstValueFrom(this.api.getStudents(schoolId));
      this.groupStudents();
    } catch (err) { console.warn('Failed to load students/stages', err); }
    finally { this.loading = false; }
  }

  private groupStudents(): void {
    this.grouped = {};
    // create keys for stages
    for (const s of this.stages) this.grouped[s.stageId] = [];
    this.grouped['unassigned'] = [];
    for (const st of this.students) {
      const key = (st as any).stageId ?? (st as any).stage ?? (st as any).level ?? null;
      if (key && this.grouped[key]) this.grouped[key].push(st);
      else if (key && typeof key === 'string') {
        // create a dynamic group for string-level name
        if (!this.grouped[key]) this.grouped[key] = [];
        this.grouped[key].push(st);
      } else this.grouped['unassigned'].push(st);
    }
  }

  toggleSelect(userId: string): void { if (this.selected.has(userId)) this.selected.delete(userId); else this.selected.add(userId); }

  async deleteSelected(): Promise<void> {
    if (this.selected.size === 0) return;
    const alert = await this.alertCtrl.create({ header: 'Delete students', message: `Delete ${this.selected.size} selected student(s)?`, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Delete', role: 'destructive', handler: async () => { await this.performDeleteSelected(); } } ] });
    await alert.present();
  }

  async assignStageToSelected(): Promise<void> {
    if (this.selected.size === 0) return;
    const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) return;
    const inputs: any[] = [];
    inputs.push({ name: 'stageId', type: 'radio', label: 'None', value: '' , checked: true });
    for (const st of this.stages) inputs.push({ name: 'stageId', type: 'radio', label: st.name, value: st.stageId, checked: false });
    const alert = await this.alertCtrl.create({ header: 'Assign Stage to selected students', inputs, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: async (val: any) => {
      try {
        // if a stage was selected, check for sublevels
        if (val && val !== '') {
          try {
            const subs = await firstValueFrom(this.api.getSubLevels(schoolId, val));
            if (subs && subs.length) {
              const subInputs: any[] = [];
              subInputs.push({ name: 'subLevelId', type: 'radio', label: 'None', value: '' , checked: true });
              for (const sl of subs) subInputs.push({ name: 'subLevelId', type: 'radio', label: sl.name, value: sl.subLevelId, checked: false });
              const subAlert = await this.alertCtrl.create({ header: 'Assign Sublevel (optional)', inputs: subInputs, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: async (sv: any) => { await this.performAssignStageToSelected(val, sv || ''); } } ] });
              await subAlert.present();
              return true;
            }
          } catch (e) { console.warn('Failed to load sublevels', e); }
        }
        // no sublevels or none selected
        await this.performAssignStageToSelected(val || '');
      } catch (err) {
        console.error('assign stage to selected failed', err);
        const t = await this.toastCtrl.create({ message: 'Failed to assign stage', duration: 2200, color: 'danger' }); await t.present();
      }
      return true;
    } } ] });
    await alert.present();
  }

  private async performAssignStageToSelected(stageId: string, subLevelId?: string): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      const ids = Array.from(this.selected);
      const failures: string[] = [];
      for (const id of ids) {
        try { await firstValueFrom(this.api.updateStudent(schoolId, id, { stageId: stageId || null, subLevelId: subLevelId || null } as any)); } catch (err) { failures.push(id); }
      }
      if (failures.length === 0) {
        const t = await this.toastCtrl.create({ message: 'Stage assigned', duration: 1400, color: 'success' }); await t.present();
      } else {
        const t = await this.toastCtrl.create({ message: `Failed for ${failures.length} student(s)`, duration: 2200, color: 'warning' }); await t.present();
      }
      this.selected.clear();
      await this.loadAll();
    } catch (err) {
      console.error('performAssignStageToSelected error', err);
      const t = await this.toastCtrl.create({ message: 'Failed to assign stage', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  private async performDeleteSelected(): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      const ids = Array.from(this.selected);
      const failures: string[] = [];
      for (const id of ids) {
        try { await firstValueFrom(this.api.deleteStudent(schoolId, id)); } catch (err) { failures.push(id); }
      }
      if (failures.length === 0) {
        const t = await this.toastCtrl.create({ message: 'Students deleted', duration: 1400, color: 'success' }); await t.present();
      } else {
        const t = await this.toastCtrl.create({ message: `Failed to delete ${failures.length} student(s)`, duration: 2200, color: 'warning' }); await t.present();
      }
      this.selected.clear();
      await this.loadAll();
    } catch (err) {
      console.error('delete selected failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to delete students', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async editStudentName(s: Student): Promise<void> {
    const alert = await this.alertCtrl.create({ header: 'Edit name', inputs: [ { name: 'firstName', type: 'text', value: s.firstName || '', placeholder: 'First name' }, { name: 'lastName', type: 'text', value: s.lastName || '', placeholder: 'Last name' } ], buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: async (vals: any) => { await this.performUpdateStudent(s.userId, vals); } } ] });
    await alert.present();
  }

  async assignStage(s: Student): Promise<void> {
    const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) return;
    const inputs: any[] = [];
    inputs.push({ name: 'stageId', type: 'radio', label: 'None', value: '' , checked: !s.stageId });
    for (const st of this.stages) {
      inputs.push({ name: 'stageId', type: 'radio', label: st.name, value: st.stageId, checked: s.stageId === st.stageId });
    }
    const alert = await this.alertCtrl.create({ header: 'Assign Stage', inputs, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: async (val: any) => {
        try {
          // if a stage was selected, check if it has sublevels
          if (val && val !== '') {
            try {
              const subs = await firstValueFrom(this.api.getSubLevels(schoolId, val));
              if (subs && subs.length) {
                // ask for sublevel selection
                const subInputs: any[] = [];
                subInputs.push({ name: 'subLevelId', type: 'radio', label: 'None', value: '' , checked: !s.stageId });
                for (const sl of subs) subInputs.push({ name: 'subLevelId', type: 'radio', label: sl.name, value: sl.subLevelId, checked: (s as any).subLevelId === sl.subLevelId });
                const subAlert = await this.alertCtrl.create({ header: 'Assign Sublevel (optional)', inputs: subInputs, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: async (sv: any) => {
                    try {
                      await firstValueFrom(this.api.updateStudent(schoolId, s.userId, { stageId: val, subLevelId: sv || '' } as any));
                      const t = await this.toastCtrl.create({ message: 'Stage updated', duration: 1400, color: 'success' }); await t.present();
                      await this.loadAll();
                      try { window.dispatchEvent(new CustomEvent('students:updated', { detail: { schoolId } })); } catch {}
                    } catch (err) {
                      console.error('assign stage failed', err);
                      const t = await this.toastCtrl.create({ message: 'Failed to update stage', duration: 2200, color: 'danger' }); await t.present();
                    }
                } } ] });
                await subAlert.present();
                return true;
              }
            } catch (e) {
              console.warn('Failed to load sublevels for stage', e);
            }
          }

          // fallback: no sublevels or none selected
          await firstValueFrom(this.api.updateStudent(schoolId, s.userId, { stageId: val } as any));
          const t = await this.toastCtrl.create({ message: 'Stage updated', duration: 1400, color: 'success' }); await t.present();
          await this.loadAll();
          // notify other parts of the app (dashboard) that student stage assignments changed
          try { window.dispatchEvent(new CustomEvent('students:updated', { detail: { schoolId } })); } catch {}
        } catch (err) {
          console.error('assign stage failed', err);
          const t = await this.toastCtrl.create({ message: 'Failed to update stage', duration: 2200, color: 'danger' }); await t.present();
        }
        return true;
      } } ] });
    await alert.present();
  }

  private async performUpdateStudent(userId: string, vals: any): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.updateStudent(schoolId, userId, { firstName: (vals?.firstName||'').trim() || null, lastName: (vals?.lastName||'').trim() || null }));
      const t = await this.toastCtrl.create({ message: 'Student updated', duration: 1400, color: 'success' }); await t.present();
      await this.loadAll();
    } catch (err) {
      console.error('update student failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to update student', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async confirmDeleteStudent(userId: string): Promise<void> {
    const alert = await this.alertCtrl.create({ header: 'Delete student', message: 'Delete this student?', buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Delete', role: 'destructive', handler: async () => { await this.performSingleDelete(userId); } } ] });
    await alert.present();
  }

  async editSelected(): Promise<void> {
    if (this.selected.size !== 1) {
      const t = await this.toastCtrl.create({ message: 'Select exactly one student to edit', duration: 1400, color: 'warning' });
      await t.present();
      return;
    }
    const id = Array.from(this.selected)[0];
    const s = this.students.find(x => x.userId === id);
    if (!s) {
      const t = await this.toastCtrl.create({ message: 'Selected student not found', duration: 1400, color: 'danger' });
      await t.present();
      return;
    }
    await this.editStudentName(s);
  }

  private async performSingleDelete(userId: string): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.deleteStudent(schoolId, userId));
      const t = await this.toastCtrl.create({ message: 'Student deleted', duration: 1400, color: 'success' }); await t.present();
      await this.loadAll();
    } catch (err) {
      console.error('delete student failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to delete student', duration: 2200, color: 'danger' }); await t.present();
    }
  }

}
