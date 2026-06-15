import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonButton, IonCard, IonCardContent, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, CreateLessonRequest } from '../../services/api.service';

@Component({
  selector: 'app-create-lesson',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton],
  templateUrl: './create-lesson.page.html',
  styleUrls: ['./create-lesson.page.scss'],
})
export class CreateLessonPage {
  lessonForm = this.fb.group({ courseId: ['', Validators.required], title: ['', Validators.required], bodyMarkdown: [''], resourceUrl: [''], order: [0], durationMinutes: [null] });
  busy = false; message: string | null = null; error: string | null = null;

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {}

  async createLesson(): Promise<void> {
    this.busy = true; this.error = null; this.message = null;
    try {
      const { courseId, ...rest } = this.lessonForm.value as { courseId: string } & CreateLessonRequest;
      const res = await firstValueFrom(this.api.createLesson(courseId, rest));
      this.message = `Lesson created: ${res.lessonId}`;
      setTimeout(() => this.router.navigateByUrl('/dashboard'), 700);
    } catch (err:any) { this.error = err instanceof Error ? err.message : 'Failed'; }
    finally { this.busy = false; }
  }
}
