import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonButton, IonCard, IonCardContent, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreateCourseRequest } from '../../services/api.service';

@Component({
  selector: 'app-create-course',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton],
  templateUrl: './create-course.page.html',
  styleUrls: ['./create-course.page.scss'],
})
export class CreateCoursePage {
  courseForm = this.fb.group({ title: ['', Validators.required], summary: [''], level: [''], subjectId: [''] });
  busy = false; message: string | null = null; error: string | null = null;

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {}

  async createCourse(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const schoolId = this.api.loadAuth()?.schoolId; if (!schoolId) throw new Error('Missing schoolId');
      const payload = this.courseForm.value as CreateCourseRequest & { subjectId?: string | null };

      if (payload.subjectId && payload.subjectId.trim().length > 0) {
        // teacher-scoped creation: teachers assigned to the subject are allowed
        const subjectId = payload.subjectId.trim();
        const res = await firstValueFrom(this.api.createCourseForSubject(schoolId, subjectId, payload));
        this.message = `Course created: ${(res as any).courseId}`;
      } else {
        // school-level creation requires HeadTeacher/Admin — may return 401 for ordinary teachers
        const res = await firstValueFrom(this.api.createCourse(schoolId, payload));
        this.message = `Course created: ${(res as any).courseId}`;
      }

      setTimeout(() => this.router.navigateByUrl('/dashboard'), 700);
    } catch (err:any) {
      // surface the friendly message produced by ApiService.process
      this.error = err instanceof Error ? (err.message ?? String(err)) : 'Failed';
    } finally {
      this.busy = false;
    }
  }
}
