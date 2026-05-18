import { Component, OnInit } from '@angular/core';

import { IonicModule, AlertController, ToastController } from '@ionic/angular';
import { ApiService, StageDetails } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-manage-levels',
    imports: [IonicModule],
    templateUrl: './manage-levels.page.html',
    styleUrls: ['./manage-levels.page.scss']
})
export class ManageLevelsPage implements OnInit {
  stages: StageDetails[] = [];
  loading = false;
  sublevels: Record<string, Array<{ subLevelId: string; name: string; label?: string | null; description?: string | null }>> = {};
  // currently selected sublevel per stage (for dropdown selection)
  selectedSubLevel: Record<string, string | null> = {};
  private _stageListener: any;

  constructor(private api: ApiService, private alertCtrl: AlertController, private toastCtrl: ToastController) {}

  ngOnInit(): void {
    void this.loadStages();
    // refresh when other components create a stage
    this._stageListener = (_ev: any) => { void this.loadStages(); };
    try { window.addEventListener('stages:created', this._stageListener); } catch {}
  }

  ngOnDestroy(): void {
    try { window.removeEventListener('stages:created', this._stageListener); } catch {}
  }

  async loadStages(): Promise<void> {
    try {
      this.loading = true;
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId ?? null;
      if (!schoolId) {
        const t = await this.toastCtrl.create({ message: 'No school selected', duration: 1800, color: 'warning' }); await t.present();
        this.stages = [];
        return;
      }

      try {
        this.stages = await firstValueFrom(this.api.getStages(schoolId));
        // pre-load sublevels for each stage (non-blocking)
        for (const st of this.stages) {
          void this.loadSubLevelsForStage(st.stageId);
        }
      } catch (err: any) {
        // show a helpful message when user is not authorized
        const status = (err && (err as any).status) ?? null;
        if (status === 401) {
          const t = await this.toastCtrl.create({ message: 'You are not authorized to manage levels. Please sign in as the head teacher.', duration: 3000, color: 'warning' }); await t.present();
        } else {
          const t = await this.toastCtrl.create({ message: `Failed to load levels: ${err?.message ?? 'Request failed'}`, duration: 3000, color: 'danger' }); await t.present();
        }
        this.stages = [];
      }
    } catch (err) {
      console.warn('Failed to load stages', err);
    } finally { this.loading = false; }
  }

  async loadSubLevelsForStage(stageId: string): Promise<void> {
    try {
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId ?? null; if (!schoolId) return;
      const subs = await firstValueFrom(this.api.getSubLevels(schoolId, stageId));
      this.sublevels[stageId] = subs ?? [];
      // default selected to first sublevel if none selected
      if (!this.selectedSubLevel[stageId]) this.selectedSubLevel[stageId] = (this.sublevels[stageId] && this.sublevels[stageId].length) ? this.sublevels[stageId][0].subLevelId : null;
    } catch (err) {
      console.warn('Failed to load sublevels for', stageId, err);
      this.sublevels[stageId] = [];
      this.selectedSubLevel[stageId] = null;
    }
  }

  onSubLevelSelected(stageId: string, subLevelId: string | null): void {
    this.selectedSubLevel[stageId] = subLevelId ?? null;
  }

  getSubLevelById(stageId: string, id: string | null) {
    if (!id) return null;
    return (this.sublevels[stageId] || []).find(s => s.subLevelId === id) ?? null;
  }

  async addSubLevel(s: StageDetails): Promise<void> {
    const alert = await this.alertCtrl.create({
      header: `Add sublevel for ${s.name}`,
      inputs: [
        { name: 'name', type: 'text', placeholder: 'Sublevel name (eg. Grade 1)' },
        { name: 'label', type: 'text', placeholder: 'Short label (optional)' },
        { name: 'description', type: 'text', placeholder: 'Description (optional)' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Create', handler: (vals: any) => {
          const name = (vals?.name || '').trim();
          if (!name) { this.toastCtrl.create({ message: 'Name required', duration: 1600, color: 'warning' }).then(t=>t.present()); return false; }
          void this.createSubLevelFromVals(s, vals);
          return true;
        } }
      ]
    });
    await alert.present();
  }

  private async createSubLevelFromVals(s: StageDetails, vals: any): Promise<void> {
    try {
      const name = (vals?.name || '').trim();
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.createSubLevel(schoolId, s.stageId, { name, label: (vals?.label||'').trim() || null, description: (vals?.description||'').trim() || null }));
      const t = await this.toastCtrl.create({ message: 'Sublevel created', duration: 1400, color: 'success' }); await t.present();
      await this.loadSubLevelsForStage(s.stageId);
    } catch (err) {
      console.error('create sublevel failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to create sublevel', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async editSubLevel(s: StageDetails, sl: { subLevelId: string; name: string; label?: string | null; description?: string | null }): Promise<void> {
    const alert = await this.alertCtrl.create({
      header: 'Edit sublevel',
      inputs: [
        { name: 'name', type: 'text', value: sl.name || '', placeholder: 'Sublevel name' },
        { name: 'label', type: 'text', value: sl.label || '', placeholder: 'Short label (optional)' },
        { name: 'description', type: 'text', value: sl.description || '', placeholder: 'Description (optional)' }
      ],
      buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Save', handler: (vals: any) => { const name = (vals?.name||'').trim(); if (!name) { this.toastCtrl.create({ message: 'Name required', duration: 1600, color: 'warning' }).then(t=>t.present()); return false; } void this.saveSubLevelFromVals(s, sl, vals); return true; } } ]
    });
    await alert.present();
  }

  private async saveSubLevelFromVals(s: StageDetails, sl: any, vals: any): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.updateSubLevel(schoolId, s.stageId, sl.subLevelId, { name: (vals?.name||'').trim(), label: (vals?.label||'').trim() || null, description: (vals?.description||'').trim() || null }));
      const t = await this.toastCtrl.create({ message: 'Sublevel updated', duration: 1400, color: 'success' }); await t.present();
      await this.loadSubLevelsForStage(s.stageId);
    } catch (err) {
      console.error('update sublevel failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to update sublevel', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async deleteSubLevelConfirm(s: StageDetails, sl: { subLevelId: string; name: string }): Promise<void> {
    const alert = await this.alertCtrl.create({ header: 'Delete sublevel', message: `Delete "${sl.name}"?`, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Delete', role: 'destructive', handler: async () => { await this.deleteSubLevel(s, sl); } } ] });
    await alert.present();
  }

  async deleteSubLevel(s: StageDetails, sl: { subLevelId: string; name: string }): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.deleteSubLevel(schoolId, s.stageId, sl.subLevelId));
      const t = await this.toastCtrl.create({ message: 'Sublevel deleted', duration: 1400, color: 'success' }); await t.present();
      await this.loadSubLevelsForStage(s.stageId);
    } catch (err) {
      console.error('delete sublevel failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to delete sublevel', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async addStage(): Promise<void> {
    const alert = await this.alertCtrl.create({
      header: 'Add level',
      inputs: [
        { name: 'name', type: 'text', placeholder: 'Level name (eg. Beginner)' },
        { name: 'label', type: 'text', placeholder: 'Short label (optional)' },
        { name: 'description', type: 'text', placeholder: 'Description (optional)' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Create', handler: (vals: any) => {
          const name = (vals?.name || '').trim();
          if (!name) { this.toastCtrl.create({ message: 'Name required', duration: 1600, color: 'warning' }).then(t=>t.present()); return false; }
          // delegate async work to keep handler synchronous
          void this.createStageFromVals(vals);
          return true;
        }}
      ]
    });
    await alert.present();
  }

  private async createStageFromVals(vals: any): Promise<void> {
    try {
      const name = (vals?.name || '').trim();
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId ?? null;
      if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.createStage(schoolId, { name, label: (vals?.label||'').trim() || null, description: (vals?.description||'').trim() || null }));
      const t = await this.toastCtrl.create({ message: 'Level created', duration: 1400, color: 'success' }); await t.present();
      await this.loadStages();
    } catch (err) {
      console.error('create stage failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to create level', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async editStage(s: StageDetails): Promise<void> {
    const alert = await this.alertCtrl.create({
      header: 'Edit level',
      inputs: [
        { name: 'name', type: 'text', value: s.name || '', placeholder: 'Level name' },
        { name: 'label', type: 'text', value: s.label || '', placeholder: 'Short label (optional)' },
        { name: 'description', type: 'text', value: s.description || '', placeholder: 'Description (optional)' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Save', handler: (vals: any) => {
          const name = (vals?.name || '').trim();
          if (!name) { this.toastCtrl.create({ message: 'Name required', duration: 1600, color: 'warning' }).then(t=>t.present()); return false; }
          void this.saveStageFromVals(s, vals);
          return true;
        }}
      ]
    });
    await alert.present();
  }

  private async saveStageFromVals(s: StageDetails, vals: any): Promise<void> {
    try {
      const name = (vals?.name || '').trim();
      const auth = this.api.loadAuth();
      const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.updateStage(schoolId, s.stageId, { name, label: (vals?.label||'').trim() || null, description: (vals?.description||'').trim() || null }));
      const t = await this.toastCtrl.create({ message: 'Level updated', duration: 1400, color: 'success' }); await t.present();
      await this.loadStages();
    } catch (err) {
      console.error('update stage failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to update level', duration: 2200, color: 'danger' }); await t.present();
    }
  }

  async deleteStageConfirm(s: StageDetails): Promise<void> {
    const alert = await this.alertCtrl.create({ header: 'Delete level', message: `Delete "${s.name}"? This action cannot be undone.`, buttons: [ { text: 'Cancel', role: 'cancel' }, { text: 'Delete', role: 'destructive', handler: async () => { await this.deleteStage(s); } } ] });
    await alert.present();
  }

  async deleteStage(s: StageDetails): Promise<void> {
    try {
      const auth = this.api.loadAuth(); const schoolId = auth?.schoolId ?? null; if (!schoolId) throw new Error('No school');
      await firstValueFrom(this.api.deleteStage(schoolId, s.stageId));
      const t = await this.toastCtrl.create({ message: 'Level deleted', duration: 1400, color: 'success' }); await t.present();
      await this.loadStages();
    } catch (err) {
      console.error('delete stage failed', err);
      const t = await this.toastCtrl.create({ message: 'Failed to delete level', duration: 2200, color: 'danger' }); await t.present();
    }
  }
}
