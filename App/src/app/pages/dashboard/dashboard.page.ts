import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCol,
  IonContent,
  IonGrid,
  IonHeader,
  IonIcon,
  IonInput,
  IonItem,
  IonLabel,
  IonRow,
  IonSelect,
  IonSelectOption,
  IonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { addIcons } from 'ionicons';
import { logOutOutline, schoolOutline, peopleOutline, libraryOutline, bookOutline, personAddOutline } from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { ApiService, AssignTeacherSubjectRequest, CreateCourseRequest, CreateLessonRequest, CreatePersonRequest, CreateSubjectRequest } from '../../services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonGrid,
    IonRow,
    IonCol,
    IonCard,
    IonCardContent,
    IonItem,
    IonLabel,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonButton,
    IonText,
    IonIcon,
  ],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.scss'],
})
export class DashboardPage implements OnInit {
  auth = this.api.loadAuth();
  busy = false;
  message: string | null = null;
  error: string | null = null;
  teachers: Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }> = [];
  subjects: Array<{ subjectId: string; name: string; stage: number; description?: string | null }> = [];

  // loading / error states for lazy-loading selects
  teachersLoading = false;
  subjectsLoading = false;
  teachersError: string | null = null;
  subjectsError: string | null = null;

  stages = [
    { value: 0, label: 'Early' },
    { value: 1, label: 'Primary' },
    { value: 2, label: 'Secondary' },
    { value: 3, label: 'Higher' },
  ];

  subjectForm = this.fb.group({
    name: ['', Validators.required],
    stage: [this.stages[1].value, Validators.required],
    description: [''],
  });

  teacherForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
  });

  studentForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
  });

  assignForm = this.fb.group({
    teacherId: ['', Validators.required],
    subjectId: ['', Validators.required],
  });

  courseForm = this.fb.group({
    title: ['', Validators.required],
    summary: [''],
    level: [''],
    subjectId: [''],
  });

  lessonForm = this.fb.group({
    courseId: ['', Validators.required],
    title: ['', Validators.required],
    bodyMarkdown: [''],
    resourceUrl: [''],
    order: [0],
    durationMinutes: [null],
  });

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {
    addIcons({ logOutOutline, schoolOutline, peopleOutline, libraryOutline, bookOutline, personAddOutline });
  }

  ngOnInit(): void {
    console.log('Dashboard init, loaded auth:', this.auth);
    if (!this.auth?.token) {
      console.log('No auth token, redirecting to /auth');
      this.router.navigateByUrl('/auth');
    }
  }

  // Lazy-load teachers when select opens
  async onTeacherSelectOpen(): Promise<void> {
    if (this.teachers.length || this.teachersLoading) return;
    this.teachersLoading = true;
    this.teachersError = null;
    try {
      const schoolId = this.requireSchool();
      this.teachers = await firstValueFrom(this.api.getTeachers(schoolId));
    } catch (err: any) {
      this.teachersError = err instanceof Error ? err.message : 'Failed to load teachers';
      console.warn('Failed loading teachers', err);
      if ((err as any)?.status === 401) {
        this.api.clearAuth();
        this.router.navigateByUrl('/auth');
      }
    } finally {
      this.teachersLoading = false;
    }
  }

  // Lazy-load subjects when select opens
  async onSubjectSelectOpen(): Promise<void> {
    if (this.subjects.length || this.subjectsLoading) return;
    this.subjectsLoading = true;
    this.subjectsError = null;
    try {
      const schoolId = this.requireSchool();
      this.subjects = await firstValueFrom(this.api.getSubjects(schoolId));
    } catch (err: any) {
      this.subjectsError = err instanceof Error ? err.message : 'Failed to load subjects';
      console.warn('Failed loading subjects', err);
      if ((err as any)?.status === 401) {
        this.api.clearAuth();
        this.router.navigateByUrl('/auth');
      }
    } finally {
      this.subjectsLoading = false;
    }
  }

  logout(): void {
    this.api.clearAuth();
    this.router.navigateByUrl('/auth');
  }

  createSubject(): void {
    this.run(async () => {
      const schoolId = this.requireSchool();
      const payload = this.subjectForm.value as CreateSubjectRequest;
      const res = await firstValueFrom(this.api.createSubject(schoolId, payload));
      this.message = `Subject created: ${res.subjectId}`;
      // Auto-fill the assign form so head teacher can immediately assign this subject
      try { this.assignForm.get('subjectId')?.setValue(res.subjectId); } catch {}
    });
  }

  createTeacher(): void {
    this.run(async () => {
      const schoolId = this.requireSchool();
      const payload = this.teacherForm.value as CreatePersonRequest;
      const res = await firstValueFrom(this.api.createTeacher(schoolId, payload));
      // Show both username and userId so admin can confirm the created identity
      this.message = `Teacher created: ${res.username} (${res.userId})`;
      // Auto-fill the assign form so head teacher can immediately assign this teacher
      try { this.assignForm.get('teacherId')?.setValue(res.userId); } catch {}
    });
  }

  createStudent(): void {
    this.run(async () => {
      const schoolId = this.requireSchool();
      const payload = this.studentForm.value as CreatePersonRequest;
      const res = await firstValueFrom(this.api.createStudent(schoolId, payload));
      this.message = `Student created: ${res.username}`;
    });
  }

  assignTeacher(): void {
    this.run(async () => {
      const schoolId = this.requireSchool();
      const payload = this.assignForm.value as AssignTeacherSubjectRequest;
      // basic client-side validation: ensure values look like GUIDs
      if (!this.isGuid(payload.teacherId) || !this.isGuid(payload.subjectId)) {
        throw new Error('Please select a valid teacher and subject.');
      }
      const res = await firstValueFrom(this.api.assignTeacherSubject(schoolId, payload));
      this.message = `Teacher ${res.teacherId} assigned to subject ${res.subjectId}`;
    });
  }

  private isGuid(value: string | null | undefined): boolean {
    if (!value) return false;
    const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;
    return guidRegex.test(value);
  }

  createCourse(): void {
    this.run(async () => {
      const schoolId = this.requireSchool();
      const payload = this.courseForm.value as CreateCourseRequest;
      const res = await firstValueFrom(this.api.createCourse(schoolId, payload));
      this.message = `Course created: ${res.courseId}`;
    });
  }

  createLesson(): void {
    this.run(async () => {
      const { courseId, ...rest } = this.lessonForm.value as { courseId: string } & CreateLessonRequest;
      const res = await firstValueFrom(this.api.createLesson(courseId, rest));
      this.message = `Lesson created: ${res.lessonId}`;
    });
  }

  private requireSchool(): string {
    const schoolId = this.api.loadAuth()?.schoolId;
    if (!schoolId) throw new Error('Missing schoolId. Please login again.');
    return schoolId;
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.error = null;
    this.message = null;
    try {
      await action();
    } catch (err) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally {
      this.busy = false;
    }
  }
}
