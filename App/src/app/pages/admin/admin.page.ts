import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonCard,
  IonCardContent,
  IonItem,
  IonLabel,
  IonInput,
  IonButton,
  IonText,
} from '@ionic/angular/standalone';
import { ApiService, CreatePersonRequest, CreateSubjectRequest } from '../../services/api.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText],
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Admin Dashboard</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="admin-shell">
      <ion-card>
        <ion-card-content>
          <h2>Admin: manage basic entities</h2>
          <p>Admins can create students, teachers and subjects.</p>
        </ion-card-content>
      </ion-card>

      <ion-card>
        <ion-card-content>
          <p class="panel-title">Create student</p>
          <form [formGroup]="studentForm" (ngSubmit)="createStudent()">
            <ion-item>
              <ion-label position="stacked">Username</ion-label>
              <ion-input formControlName="username"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Password</ion-label>
              <ion-input type="password" formControlName="password"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">First name</ion-label>
              <ion-input formControlName="firstName"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Last name</ion-label>
              <ion-input formControlName="lastName"></ion-input>
            </ion-item>
            <ion-button expand="block" type="submit" [disabled]="busy">Create student</ion-button>
          </form>
          <div *ngIf="message"><ion-text color="success">{{ message }}</ion-text></div>
          <div *ngIf="error"><ion-text color="danger">{{ error }}</ion-text></div>
        </ion-card-content>
      </ion-card>

      <ion-card>
        <ion-card-content>
          <p class="panel-title">Create teacher</p>
          <form [formGroup]="teacherForm" (ngSubmit)="createTeacher()">
            <ion-item>
              <ion-label position="stacked">Username</ion-label>
              <ion-input formControlName="username"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Password</ion-label>
              <ion-input type="password" formControlName="password"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">First name</ion-label>
              <ion-input formControlName="firstName"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Last name</ion-label>
              <ion-input formControlName="lastName"></ion-input>
            </ion-item>
            <ion-button expand="block" type="submit" [disabled]="busy">Create teacher</ion-button>
          </form>
        </ion-card-content>
      </ion-card>

      <ion-card>
        <ion-card-content>
          <p class="panel-title">Create subject</p>
          <form [formGroup]="subjectForm" (ngSubmit)="createSubject()">
            <ion-item>
              <ion-label position="stacked">Name</ion-label>
              <ion-input formControlName="name"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Stage (number)</ion-label>
              <ion-input type="number" formControlName="stage"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Description</ion-label>
              <ion-input formControlName="description"></ion-input>
            </ion-item>
            <ion-button expand="block" type="submit" [disabled]="busy">Create subject</ion-button>
          </form>
        </ion-card-content>
      </ion-card>
    </ion-content>
  `,
  styles: [
    `:host { display: block; padding: 16px; }
     .panel-title { font-weight: 600; margin-bottom: 8px; }`
  ],
})
export class AdminPage {
  busy = false;
  message: string | null = null;
  error: string | null = null;

  studentForm = this.fb.group({ username: ['', Validators.required], password: ['', [Validators.required, Validators.minLength(8)]], firstName: ['', Validators.required], lastName: ['', Validators.required] });
  teacherForm = this.fb.group({ username: ['', Validators.required], password: ['', [Validators.required, Validators.minLength(8)]], firstName: ['', Validators.required], lastName: ['', Validators.required] });
  subjectForm = this.fb.group({ name: ['', Validators.required], stage: [1, Validators.required], description: [''] });

  constructor(private fb: FormBuilder, private api: ApiService) {}

  private requireSchool(): string {
    const schoolId = this.api.loadAuth()?.schoolId;
    if (!schoolId) throw new Error('Missing schoolId. Please login again.');
    return schoolId;
  }

  async createStudent(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.studentForm.value as CreatePersonRequest;
      const payload: CreatePersonRequest = {
        ...raw,
        username: (raw.username || '').trim().substring(0, 15),
      };
      const res = await this.api.createStudent(this.requireSchool(), payload).toPromise();
      const username = res?.username ?? '<unknown>';
      this.message = `Student created: ${username}`;
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }

  async createTeacher(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.teacherForm.value as CreatePersonRequest;
      const payload: CreatePersonRequest = {
        ...raw,
        username: (raw.username || '').trim().substring(0, 15),
      };
      const res = await this.api.createTeacher(this.requireSchool(), payload).toPromise();
      const username = res?.username ?? '<unknown>';
      this.message = `Teacher created: ${username}`;
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }

  async createSubject(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const payload = this.subjectForm.value as CreateSubjectRequest;
      const res = await this.api.createSubject(this.requireSchool(), payload).toPromise();
      const subjectId = res?.subjectId ?? '<unknown>';
      this.message = `Subject created: ${subjectId}`;
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }
}
