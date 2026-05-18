import { Component, OnInit } from '@angular/core';

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
  IonChip,
  IonIcon,
} from '@ionic/angular/standalone';
import { IonSelect, IonSelectOption, IonTextarea } from '@ionic/angular/standalone';
import { ApiService, CreatePersonRequest, CreateSubjectRequest } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-admin',
    imports: [ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText, IonChip, IonIcon, IonSelect, IonSelectOption, IonTextarea],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Admin Dashboard</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="admin-shell">
      @if (schoolCreated) {
        <ion-card>
          <ion-card-content>
            <div style="display:flex;align-items:center;justify-content:space-between">
              <div><strong>School:</strong> {{ schoolName }}</div>
              <div>
                <ion-button fill="clear" (click)="openEdit()">
                  <ion-icon name="settings-outline"></ion-icon>
                </ion-button>
              </div>
            </div>
          </ion-card-content>
        </ion-card>
      }
    
      @if (editing) {
        <ion-card>
          <ion-card-content>
            <p class="panel-title">Edit school</p>
            <form [formGroup]="editForm" (ngSubmit)="saveEdit()">
              <ion-item>
                <ion-label position="stacked">School name</ion-label>
                <ion-input formControlName="schoolName"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Address</ion-label>
                <ion-input formControlName="schoolAddress"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">City</ion-label>
                <ion-input formControlName="city"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">State</ion-label>
                <ion-input formControlName="state"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Country</ion-label>
                <ion-input formControlName="country"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">County</ion-label>
                <ion-input formControlName="county"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Zip code</ion-label>
                <ion-input formControlName="zipCode"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Phone</ion-label>
                <ion-input formControlName="phoneNumber"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Email</ion-label>
                <ion-input formControlName="email"></ion-input>
              </ion-item>
              <ion-item>
                <ion-label position="stacked">Description</ion-label>
                <ion-input formControlName="description"></ion-input>
              </ion-item>
              <div style="display:flex;gap:8px;margin-top:12px">
                <ion-button type="submit" [disabled]="busy">Save</ion-button>
                <ion-button fill="clear" (click)="editing = false">Cancel</ion-button>
              </div>
            </form>
          </ion-card-content>
        </ion-card>
      }
      <section class="hero">
        <div class="hero-text">
          <p class="eyebrow">Administration</p>
          <h1>Manage your school</h1>
          <p class="lede">Create teachers, students and subjects quickly. Assign teachers to subjects and keep things organised.</p>
          <div class="chips">
            <ion-chip color="light" class="chip-soft">
              <ion-icon name="people-outline"></ion-icon>
              <ion-label>Teachers</ion-label>
            </ion-chip>
            <ion-chip color="light" class="chip-soft">
              <ion-icon name="school-outline"></ion-icon>
              <ion-label>Subjects</ion-label>
            </ion-chip>
          </div>
        </div>
      </section>
    
      <ion-card>
        <ion-card-content>
          <p class="panel-title">Create school</p>
          <form [formGroup]="schoolForm" (ngSubmit)="createSchool()">
            <ion-item>
              <ion-label position="stacked">School name</ion-label>
              <ion-input formControlName="schoolName"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Address</ion-label>
              <ion-input formControlName="schoolAddress"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">City</ion-label>
              <ion-input formControlName="city"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">State</ion-label>
              <ion-input formControlName="state"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Country</ion-label>
              <ion-input formControlName="country"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">County</ion-label>
              <ion-input formControlName="county"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Zip code</ion-label>
              <ion-input formControlName="zipCode"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Phone</ion-label>
              <ion-input formControlName="phoneNumber"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Email</ion-label>
              <ion-input formControlName="email"></ion-input>
            </ion-item>
            <ion-item>
              <ion-label position="stacked">Description</ion-label>
              <ion-input formControlName="description"></ion-input>
            </ion-item>
            <ion-button expand="block" type="submit" [disabled]="busy || schoolCreated">Create school</ion-button>
          </form>
        </ion-card-content>
      </ion-card>
    
      <ion-card id="students">
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
            @if (sublevels?.length) {
              <ion-item>
                <ion-label position="stacked">Sublevel (optional)</ion-label>
                <ion-select formControlName="subLevelId" placeholder="Select sublevel">
                  @for (sl of sublevels; track sl) {
                    <ion-select-option [value]="sl.subLevelId">{{ sl.name }}</ion-select-option>
                  }
                </ion-select>
              </ion-item>
            }
            <ion-button expand="block" type="submit" [disabled]="busy">Create student</ion-button>
          </form>
          @if (message) {
            <div><ion-text color="success">{{ message }}</ion-text></div>
          }
          @if (error) {
            <div><ion-text color="danger">{{ error }}</ion-text></div>
          }
        </ion-card-content>
      </ion-card>
    
      <ion-card id="teachers">
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
    
      <ion-card id="subjects">
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
            @if (sublevels?.length) {
              <ion-item>
                <ion-label position="stacked">Sublevel (optional)</ion-label>
                <ion-select formControlName="subLevelId" placeholder="Select sublevel">
                  @for (sl of sublevels; track sl) {
                    <ion-select-option [value]="sl.subLevelId">{{ sl.name }}</ion-select-option>
                  }
                </ion-select>
              </ion-item>
            }
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
        `:host { display:block }
     .admin-shell { padding: 16px; }
     .panel-title { font-weight: 600; margin-bottom: 8px }
     .hero { display:grid; grid-template-columns:1fr auto; gap:12px; background:linear-gradient(135deg,#4338ca,#7c3aed); color:#eef2ff; border-radius:14px; padding:16px; margin-bottom:12px }
     .hero-text h1 { margin:6px 0 8px }
     .lede { color:#e2e8f0 }
     .chips { display:flex; gap:8px }
     .chip-soft { --background: rgba(255,255,255,0.08); --color:#fff; }
    `
    ]
})
export class AdminPage {
  busy = false;
  message: string | null = null;
  error: string | null = null;
  schoolName: string | null = null;
  teachers: Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }> = [];
  subjects: Array<{ subjectId: string; name: string; stage: number; description?: string | null }> = [];

  studentForm = this.fb.group({ username: ['', Validators.required], password: ['', [Validators.required, Validators.minLength(8)]], firstName: ['', Validators.required], lastName: ['', Validators.required], subLevelId: [null] });
  teacherForm = this.fb.group({ username: ['', Validators.required], password: ['', [Validators.required, Validators.minLength(8)]], firstName: ['', Validators.required], lastName: ['', Validators.required], subLevelId: [null] });
  subjectForm = this.fb.group({ name: ['', Validators.required], stage: [1, Validators.required], subLevelId: [null], description: [''] });
  schoolCreated = false;
  schoolDetails: any | null = null;
  editForm = this.fb.group({
    schoolName: [''],
    description: [''],
    schoolAddress: [''],
    city: [''],
    state: [''],
    country: [''],
    county: [''],
    zipCode: [''],
    phoneNumber: [''],
    email: ['']
  });
  sublevels: Array<{ subLevelId: string; name: string; stageId: string }> = [];
  editing = false;
  schoolForm = this.fb.group({
    schoolName: ['', Validators.required],
    description: [''],
    schoolAddress: [''],
    city: [''],
    state: [''],
    country: [''],
    county: [''],
    zipCode: [''],
    phoneNumber: [''],
    email: [''],
  });
  assignForm = this.fb.group({ teacherId: ['', Validators.required], subjectId: ['', Validators.required] });
  courseForm = this.fb.group({ title: ['', Validators.required], summary: [''], level: [''], subjectId: [''] });
  lessonForm = this.fb.group({ courseId: ['', Validators.required], title: ['', Validators.required], bodyMarkdown: [''], resourceUrl: [''], order: [0], durationMinutes: [0] });

  constructor(private fb: FormBuilder, private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    const auth = this.api.loadAuth();
    if (auth?.schoolId) {
      this.schoolCreated = true;
      this.schoolName = (auth as any).schoolName ?? null;
      try {
        const t = await this.api.getTeachers(auth.schoolId).toPromise();
        this.teachers = t ?? [];
      } catch {}
      try {
        const s = await this.api.getSubjects(auth.schoolId).toPromise();
        this.subjects = s ?? [];
      } catch {}
      try {
        this.schoolDetails = await this.api.getSchool(auth.schoolId).toPromise();
      } catch {}
      // load all sublevels across stages for optional scoping
      (async () => {
        try {
            const schoolId = this.requireSchool();
            const stages = await firstValueFrom(this.api.getStages(schoolId));
            const all: Array<{ subLevelId: string; name: string; stageId: string }> = [];
            for (const st of stages) {
              try {
                const subs = await firstValueFrom(this.api.getSubLevels(schoolId, st.stageId));
                for (const sl of (subs || [])) all.push({ subLevelId: sl.subLevelId, name: `${st.name} / ${sl.name}`, stageId: st.stageId });
              } catch {}
            }
            this.sublevels = all;
          } catch (err) { this.sublevels = []; }
      })();
    }
  }

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

  openEdit(): void {
    const auth = this.api.loadAuth();
    if (!auth?.schoolId) return;
    this.editing = true;
    if (this.schoolDetails) {
      this.editForm.patchValue({
        schoolName: this.schoolDetails.schoolName,
        description: this.schoolDetails.description,
        schoolAddress: this.schoolDetails.schoolAddress,
        city: this.schoolDetails.city,
        state: this.schoolDetails.state,
        country: this.schoolDetails.country,
        county: this.schoolDetails.county,
        zipCode: this.schoolDetails.zipCode,
        phoneNumber: this.schoolDetails.phoneNumber,
        email: this.schoolDetails.email
      });
    }
  }

  async saveEdit(): Promise<void> {
    const auth = this.api.loadAuth();
    if (!auth?.schoolId) return;
    this.busy = true; this.error = null; this.message = null;
    try {
      const payload = this.editForm.value as any;
      const res = await this.api.updateSchool(auth.schoolId, payload).toPromise();
      if (res && res.schoolId) {
        // refresh details
        this.schoolDetails = await this.api.getSchool(res.schoolId).toPromise();
        // update local storage
        const a = this.api.loadAuth() || {};
        (a as any).schoolName = payload.schoolName;
        (a as any).schoolAddress = payload.schoolAddress;
        (a as any).city = payload.city;
        (a as any).state = payload.state;
        (a as any).country = payload.country;
        (a as any).county = payload.county;
        (a as any).zipCode = payload.zipCode;
        (a as any).phoneNumber = payload.phoneNumber;
        (a as any).email = payload.email;
        (a as any).description = payload.description;
        localStorage.setItem('nile.auth', JSON.stringify(a));
        this.schoolName = payload.schoolName;
        this.message = 'School updated';
      }
      this.editing = false;
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

  async assignTeacher(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.assignForm.value as { teacherId: string; subjectId: string };
      const payload = { teacherId: raw.teacherId, subjectId: raw.subjectId };
      const res = await this.api.assignTeacherSubject(this.requireSchool(), payload).toPromise();
      this.message = `Assigned teacher ${payload.teacherId} to subject ${payload.subjectId}`;
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }

  async createCourse(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.courseForm.value as { title: string; summary?: string; level?: string; subjectId?: string };
      const payload = { title: (raw.title || '').trim(), summary: (raw.summary || '').trim(), level: (raw.level || '').trim(), subjectId: raw.subjectId || null };
      const res = await this.api.createCourse(this.requireSchool(), payload).toPromise();
      const courseId = res?.courseId ?? '<unknown>';
      this.message = `Course created: ${courseId}`;
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }

  async createLesson(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.lessonForm.value as { courseId: string; title: string; bodyMarkdown?: string; resourceUrl?: string; order?: number; durationMinutes?: number };
      const payload = { title: (raw.title || '').trim(), bodyMarkdown: raw.bodyMarkdown || null, resourceUrl: raw.resourceUrl || null, order: raw.order ?? 0, durationMinutes: raw.durationMinutes ?? null };
      const res = await this.api.createLesson(raw.courseId, payload).toPromise();
      const lessonId = res?.lessonId ?? '<unknown>';
      this.message = `Lesson created: ${lessonId}`;
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

  async createSchool(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const raw = this.schoolForm.value as any;
      const payload = {
        schoolName: (raw.schoolName || '').trim(),
        description: (raw.description || '').trim(),
        schoolAddress: (raw.schoolAddress || '').trim(),
        city: (raw.city || '').trim(),
        state: (raw.state || '').trim(),
        country: (raw.country || '').trim(),
        county: (raw.county || '').trim(),
        zipCode: (raw.zipCode || '').trim(),
        phoneNumber: (raw.phoneNumber || '').trim(),
        email: (raw.email || '').trim(),
      };
      const res = await this.api.createSchool(payload).toPromise();
      const schoolId = res?.schoolId ?? null;
      if (schoolId) {
        const auth = this.api.loadAuth();
        if (auth) {
          (auth as any).schoolId = schoolId;
          try {
            (auth as any).schoolName = payload.schoolName;
            (auth as any).schoolAddress = payload.schoolAddress;
            (auth as any).city = payload.city;
            (auth as any).state = payload.state;
            (auth as any).country = payload.country;
            (auth as any).county = payload.county;
            (auth as any).zipCode = payload.zipCode;
            (auth as any).phoneNumber = payload.phoneNumber;
            (auth as any).email = payload.email;
            localStorage.setItem('nile.auth', JSON.stringify(auth));
          } catch {}
        }
        this.schoolCreated = true;
        this.message = `School created: ${schoolId}`;
        // after a short delay, scroll to the student form so admin can create students
        setTimeout(() => {
          try {
            const el = document.getElementById('students');
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
          } catch {}
        }, 300);
      } else {
        this.message = 'School created';
      }
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }
}
