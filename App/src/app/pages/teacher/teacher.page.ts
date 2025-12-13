import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonButton, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonGrid, IonRow, IonCol, IonText } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-teacher',
  standalone: true,
  imports: [CommonModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonGrid, IonRow, IonCol, IonButton, IonText],
  templateUrl: './teacher.page.html',
  styleUrls: ['./teacher.page.scss']
})
export class TeacherPage implements OnInit {
  auth = this.api.loadAuth();
  subjects: Array<{ subjectId: string; name: string; stage: number }> = [];

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    if (!this.auth?.token) this.router.navigateByUrl('/auth');
    this.loadSubjects();
  }

  async loadSubjects(): Promise<void> {
    try {
      const schoolId = this.auth?.schoolId;
      if (!schoolId) return;
      this.subjects = await firstValueFrom(this.api.getSubjects(schoolId));
    } catch (err) {
      console.warn('Failed loading subjects', err);
    }
  }

  logout(): void {
    this.api.clearAuth();
    this.router.navigateByUrl('/auth');
  }
}
