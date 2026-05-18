import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonicModule, PopoverController } from '@ionic/angular';
import { ApiService, CreateStageRequest } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-create-stage',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonicModule],
  template: `
  <ion-header>
    <ion-toolbar>
      <ion-title>Create Stage</ion-title>
    </ion-toolbar>
  </ion-header>
  <ion-content class="page-shell">
    <ion-card>
      <ion-card-content>
        <form [formGroup]="form" (ngSubmit)="save()">
          <ion-item>
            <ion-label position="stacked">Name</ion-label>
            <ion-input formControlName="name"></ion-input>
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Label (optional)</ion-label>
            <ion-input formControlName="label"></ion-input>
          </ion-item>
          <ion-item>
            <ion-label position="stacked">Description (optional)</ion-label>
            <ion-input formControlName="description"></ion-input>
          </ion-item>
          <div style="display:flex;gap:8px;margin-top:12px">
            <ion-button type="submit" [disabled]="busy">Create stage</ion-button>
            <ion-button fill="clear" (click)="close()">Cancel</ion-button>
          </div>
        </form>
        <div *ngIf="message"><ion-text color="success">{{ message }}</ion-text></div>
        <div *ngIf="error"><ion-text color="danger">{{ error }}</ion-text></div>
      </ion-card-content>
    </ion-card>
  </ion-content>
  `,
})
export class CreateStagePage {
  form = this.fb.group({ name: ['', Validators.required], label: [''], description: [''] });
  busy = false; message: string | null = null; error: string | null = null;

  constructor(private fb: FormBuilder, private api: ApiService, private pop: PopoverController) {}

  async save(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const payload = this.form.value as CreateStageRequest;
      const schoolId = this.api.loadAuth()?.schoolId;
      if (!schoolId) throw new Error('Missing schoolId');
      const res = await firstValueFrom(this.api.createStage(schoolId, payload));
      this.message = `Stage created: ${res.name}`;
      // close after short delay so user sees success
      setTimeout(() => this.pop.dismiss(res), 700);
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }

  close(): void { this.pop.dismiss(); }
}
