import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ApiError { message: string; details?: unknown }
export interface ApiResponse<T> { data: T | null; error?: ApiError | null }

export interface AuthResponse { token: string; userId: string; schoolId?: string | null; username: string; roles: string[]; redirectUrl?: string | null }
export interface RegisterHeadRequest { username: string; password: string; firstName: string; lastName: string; schoolName: string }
export interface LoginRequest { username: string; password: string }

export interface CreatePersonRequest { username: string; password: string; firstName: string; lastName: string }
export interface CreateSubjectRequest { name: string; stage: number; description?: string | null }
export interface AssignTeacherSubjectRequest { teacherId: string; subjectId: string }
export interface CreateCourseRequest { title: string; summary?: string | null; level?: string | null; subjectId?: string | null }
export interface CreateLessonRequest { title: string; bodyMarkdown?: string | null; resourceUrl?: string | null; order?: number; durationMinutes?: number | null }

interface StoredAuth {
  token: string;
  userId: string;
  schoolId: string | null;
  roles: string[];
  username: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiBase;
  private readonly apiKey = environment.apiKey;
  private readonly authKey = 'nile.auth';

  constructor(private http: HttpClient) {}

  // Convert HTTP errors into thrown Error instances with friendly messages
  private process<T>(obs: Observable<T>): Observable<T> {
    return obs.pipe(
      catchError((err: any) => {
        const msg = err?.error?.error?.message ?? err?.error?.message ?? err?.message ?? 'Request failed';
        const e = new Error(msg);
        try {
          // attach HTTP status when available so callers can react (e.g. 401 -> re-auth)
          (e as any).status = err?.status ?? err?.statusCode ?? (err?.error && err.error?.status) ?? null;
        } catch {}
        return throwError(() => e);
      })
    );
  }

  // -------- auth --------
  registerHead(payload: RegisterHeadRequest): Observable<AuthResponse> {
    return this.process(
      this.http
        .post<ApiResponse<AuthResponse>>(`${this.base}/auth/register-head`, payload, this.headers(false))
        .pipe(map(res => this.unwrap(res)))
    );
  }

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.process(
      this.http
        .post<ApiResponse<AuthResponse>>(`${this.base}/auth/login`, payload, this.headers(false))
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // -------- school admin --------
  createStudent(schoolId: string, payload: CreatePersonRequest): Observable<{ userId: string; username: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ userId: string; username: string }>>(`${this.base}/schools/${schoolId}/students`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createTeacher(schoolId: string, payload: CreatePersonRequest): Observable<{ userId: string; username: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ userId: string; username: string }>>(`${this.base}/schools/${schoolId}/teachers`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createSubject(schoolId: string, payload: CreateSubjectRequest): Observable<{ subjectId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ subjectId: string }>>(`${this.base}/schools/${schoolId}/subjects`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // GET lists for dashboard selects
  getTeachers(schoolId: string): Observable<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }>>>(`${this.base}/schools/${schoolId}/teachers`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getSubjects(schoolId: string): Observable<Array<{ subjectId: string; name: string; stage: number; description?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ subjectId: string; name: string; stage: number; description?: string | null }>>>(`${this.base}/schools/${schoolId}/subjects`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  assignTeacherSubject(schoolId: string, payload: AssignTeacherSubjectRequest): Observable<{ teacherId: string; subjectId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ teacherId: string; subjectId: string }>>(`${this.base}/schools/${schoolId}/assign-teacher`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createCourse(schoolId: string, payload: CreateCourseRequest): Observable<{ courseId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ courseId: string }>>(`${this.base}/schools/${schoolId}/courses`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createLesson(courseId: string, payload: CreateLessonRequest): Observable<{ lessonId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ lessonId: string }>>(`${this.base}/courses/${courseId}/lessons`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // -------- auth cache --------
  saveAuth(res: AuthResponse): void {
    // Be tolerant of different casing/shape from backend (Token vs token, UserId vs userId, etc.)
    const anyRes: any = res || {};
    const token = anyRes.token ?? anyRes.Token ?? anyRes.data?.token ?? anyRes.data?.Token ?? '';
    const userId = anyRes.userId ?? anyRes.UserId ?? anyRes.data?.userId ?? anyRes.data?.UserId ?? '';
    const schoolId = anyRes.schoolId ?? anyRes.SchoolId ?? anyRes.data?.schoolId ?? anyRes.data?.SchoolId ?? null;
    const roles = anyRes.roles ?? anyRes.Roles ?? anyRes.data?.roles ?? anyRes.data?.Roles ?? [];
    const username = anyRes.username ?? anyRes.Username ?? anyRes.data?.username ?? anyRes.data?.Username ?? '';

    const toStore: StoredAuth = {
      token: token as string,
      userId: userId as string,
      schoolId: (schoolId as string) ?? null,
      roles: (roles as string[]) ?? [],
      username: username as string,
    };

    try {
      localStorage.setItem(this.authKey, JSON.stringify(toStore));
    } catch (e) {
      console.error('Failed to save auth to localStorage', e);
    }
  }

  loadAuth(): StoredAuth | null {
    const raw = localStorage.getItem(this.authKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as StoredAuth;
    } catch {
      return null;
    }
  }

  clearAuth(): void {
    localStorage.removeItem(this.authKey);
  }

  // -------- helpers --------
  private headers(includeAuth = true): { headers: HttpHeaders } {
    const headers: Record<string, string> = {};
    if (this.apiKey) headers['x-api-key'] = this.apiKey;
    if (includeAuth) {
      const auth = this.loadAuth();
      if (auth?.token) headers['Authorization'] = `Bearer ${auth.token}`;
    }
    return { headers: new HttpHeaders(headers) };
  }

  private unwrap<T>(res: ApiResponse<T>): T {
    const anyRes = res as any;
    const data = res.data ?? anyRes?.Data ?? null;
    const error = res.error ?? anyRes?.Error ?? null;
    if (data !== null && data !== undefined) return data;
    throw new Error(error?.message ?? 'Request failed');
  }
}
