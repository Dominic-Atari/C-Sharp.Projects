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
  IonInput,
  IonItem,
  IonLabel,
  IonRow,
  IonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardContent,
    IonGrid,
    IonRow,
    IonCol,
    IonItem,
    IonLabel,
    IonInput,
    IonButton,
    IonText,
  ],
  templateUrl: './auth.page.html',
  styleUrls: ['./auth.page.scss'],
})
export class AuthPage implements OnInit {
  mode: 'login' | 'register' = 'register';
  loading = false;
  error: string | null = null;
  uid = Math.random().toString(36).slice(2, 9);

  loginForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  registerForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    schoolName: ['', [Validators.required, Validators.minLength(2)]],
  });

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    try {
      const mode = this.route.snapshot.queryParamMap.get('mode');
      if (mode === 'login' || mode === 'register') {
        this.mode = mode as 'login' | 'register';
      }
    } catch (err) {
      // ignore and keep default
    }
  }

  switchMode(mode: 'login' | 'register'): void {
    this.mode = mode;
    this.error = null;
  }

  submit(): void {
    this.error = null;
    this.loading = true;

    const onSuccess = (redirect?: string | null) => {
      this.loading = false;
      if (redirect) {
        this.router.navigateByUrl(redirect);
      } else {
        this.router.navigateByUrl('/dashboard');
      }
    };

    const onError = (err: unknown) => {
      this.loading = false;
      this.error = err instanceof Error ? err.message : 'Request failed';
    };

    if (this.mode === 'login') {
      if (this.loginForm.invalid) {
        this.loading = false;
        this.error = 'Please fill username and password (min 8 chars).';
        return;
      }
      const value = this.loginForm.value;
      // Normalize username to match server-side truncation rules (max 15 chars)
      const username = (value.username || '').trim().substring(0, 15);
      this.api
        .login({ username, password: value.password! })
        .pipe()
        .subscribe({
          next: res => {
            console.log('Login response:', res);
            // Clear any stale auth before saving the new token
            this.api.clearAuth();
            this.api.saveAuth(res);
            console.log('Saved auth:', localStorage.getItem('nile.auth'));
            try {
              // Prefer explicit redirectUrl from server when present
              const redirectFromServer = (res as any)?.redirectUrl ?? null;
              if (redirectFromServer) {
                onSuccess(redirectFromServer);
                return;
              }

              // Determine roles primarily from the server response (safer than relying on localStorage)
              // Coerce roles into a normalized array (server may return a single string or an array)
              let rolesFromResponse: any = (res as any)?.roles ?? (res as any)?.data?.roles ?? [];
              if (!rolesFromResponse) rolesFromResponse = [];
              if (typeof rolesFromResponse === 'string') rolesFromResponse = [rolesFromResponse];
              const rolesNormalized = Array.isArray(rolesFromResponse) ? rolesFromResponse.map((r: any) => String(r).toLowerCase()) : [];

              // Fallback: if response didn't include roles, use saved auth as an array safely
              let roles: string[] = [];
              if (rolesNormalized && rolesNormalized.length) {
                roles = rolesNormalized;
              } else {
                const stored = this.api.loadAuth()?.roles ?? [];
                if (typeof stored === 'string') roles = [String(stored).toLowerCase()];
                else if (Array.isArray(stored)) roles = stored.map((r: any) => String(r).toLowerCase());
                else roles = [];
              }

              // Safety net: if the server returned a token but no roles, assume learner and send to feed.
              const tokenFromRes = (res as any)?.token ?? (res as any)?.data?.token ?? null;
              if ((!roles || roles.length === 0) && tokenFromRes) {
                console.warn('Login succeeded but no roles returned — defaulting to learner landing (/feed)');
                onSuccess('/feed');
                return;
              }

              if (roles.includes('teacher')) {
                try {
                  const authNow = this.api.loadAuth();
                  const schoolId = authNow?.schoolId;
                  const userId = authNow?.userId;
                  if (schoolId && userId) {
                    this.api.getTeacherSubjects(schoolId, userId).subscribe({
                      next: subjects => {
                        if (subjects && subjects.length === 1) {
                          onSuccess(`/teacher/${subjects[0].subjectId}`);
                        } else {
                          onSuccess('/teacher');
                        }
                      },
                      error: () => onSuccess('/teacher'),
                    });
                    return;
                  }
                } catch (e) {
                  // fallback
                }
                onSuccess('/teacher');
                return;
              }
              if (roles.includes('student')) {
                // students should land on the public/home feed
                onSuccess('/feed');
                return;
              }
              if (roles.includes('headteacher')) {
                onSuccess('/dashboard');
                return;
              }
              if (roles.includes('admin')) {
                onSuccess('/admin');
                return;
              }
              // default fallback
              onSuccess('/feed');
            } catch (navErr) {
              console.error('Navigation error after login:', navErr);
              this.error = 'Navigation failed';
            }
          },
          error: err => {
            console.error('Login error:', err);
            onError(err);
          },
        });
    } else {
      if (this.registerForm.invalid) {
        this.loading = false;
        this.error = 'Please fill all fields; password must be 8+ chars.';
        return;
      }
      const value = this.registerForm.value;
      const username = (value.username || '').trim().substring(0, 15);
      this.api
        .registerHead({
          username,
          password: value.password!,
          firstName: value.firstName!,
          lastName: value.lastName!,
          schoolName: value.schoolName!,
        })
        .subscribe({
          next: res => {
            // Clear stale auth first so previous schoolId/name cannot leak into new session
            this.api.clearAuth();
            this.api.saveAuth(res);
            onSuccess((res as any)?.redirectUrl ?? null);
          },
          error: onError,
        });
    }
  }
}
