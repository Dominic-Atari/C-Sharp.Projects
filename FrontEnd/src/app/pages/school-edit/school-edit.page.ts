import { Component, OnInit } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-school-edit',
    imports: [ReactiveFormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent, IonItem, IonLabel, IonInput, IonButton, IonText],
    templateUrl: './school-edit.page.html',
    styleUrls: ['./school-edit.page.scss']
})
export class SchoolEditPage implements OnInit {
  busy = false;
  message: string | null = null;
  error: string | null = null;
  auth = this.api.loadAuth();
  school: any | null = null;

  form = this.fb.group({
    schoolName: ['', Validators.required],
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

  constructor(private fb: FormBuilder, private api: ApiService, public router: Router) {}

  ngOnInit(): void {
    if (!this.auth?.schoolId) {
      this.router.navigateByUrl('/dashboard');
      return;
    }
    firstValueFrom(this.api.getSchool(this.auth.schoolId))
      .then(s => {
        this.school = s;
        this.form.patchValue({
          schoolName: s.schoolName,
          description: s.description,
          schoolAddress: s.schoolAddress,
          city: s.city,
          state: s.state,
          country: s.country,
          county: s.county,
          zipCode: s.zipCode,
          phoneNumber: s.phoneNumber,
          email: s.email
        });
      })
      .catch(err => {
        this.error = 'Failed to load school';
      });
  }

  async save(): Promise<void> {
    if (!this.auth?.schoolId) return;
    this.busy = true; this.error = null; this.message = null;
    try {
      const payload = this.form.value as any;
      const res = await firstValueFrom(this.api.updateSchool(this.auth.schoolId, payload));
      if (res && res.schoolId) {
        this.message = 'Saved';
        this.api.mergeAuth({
          schoolName: payload.schoolName,
          schoolAddress: payload.schoolAddress,
          city: payload.city,
          state: payload.state,
          country: payload.country,
          county: payload.county,
          zipCode: payload.zipCode,
          phoneNumber: payload.phoneNumber,
          email: payload.email,
          description: payload.description,
        });
        setTimeout(() => this.router.navigateByUrl('/dashboard'), 500);
      }
    } catch (err: any) {
      this.error = err instanceof Error ? err.message : 'Request failed';
    } finally { this.busy = false; }
  }
}
