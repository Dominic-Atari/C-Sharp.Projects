import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ApiError { message: string; details?: unknown }
export interface ApiResponse<T> { data: T | null; error?: ApiError | null }

export interface AuthResponse { token: string; userId: string; schoolId?: string | null; username: string; roles: string[]; schoolName?: string | null; redirectUrl?: string | null }
export interface RegisterHeadRequest { username: string; password: string; firstName: string; lastName: string; schoolName: string }
export interface LoginRequest { username: string; password: string }

export interface CreatePersonRequest { username: string; password: string; firstName: string; lastName: string; stage?: number | string; stageId?: string | null; subLevelId?: string | null }
export interface CreateSubjectRequest { name: string; stage: number | string; description?: string | null; subLevelId?: string | null }
export interface AssignTeacherSubjectRequest { teacherId: string; subjectId: string }
export interface AssignTeacherSubjectRequest { teacherId: string; subjectId: string; subLevelId?: string | null }
export interface CreateCourseRequest { title: string; summary?: string | null; level?: string | null; subjectId?: string | null }
export interface CreateLessonRequest { title: string; bodyMarkdown?: string | null; resourceUrl?: string | null; order?: number; durationMinutes?: number | null }
export interface CreateSchoolRequest {
  schoolName: string;
  description?: string | null;
  schoolAddress?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  county?: string | null;
  zipCode?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
}

export interface CreateStageRequest {
  stageId?: string;
  name: string;
  label?: string | null;
  description?: string | null;
}

export interface StorySummary { storyId: string; schoolId: string; subjectId?: string | null; topicId?: string | null; subTopicId?: string | null; promptId?: string | null; user?: string | null; payload?: string | null; createdAt: string }
export interface CreateStoryRequest { promptId?: string | null; user?: string | null; payload?: string | null; topicId?: string | null; subTopicId?: string | null; subTopicName?: string | null }

export interface StageDetails {
  stageId: string;
  name: string;
  label?: string | null;
  description?: string | null;
}

export interface SubLevelDetails { subLevelId: string; name: string; label?: string | null; description?: string | null }
export interface CreateSubLevelRequest { subLevelId?: string; name: string; label?: string | null; description?: string | null }

export interface MembershipDetails { userId: string; username?: string; firstName?: string | null; lastName?: string | null; roleInSchool?: string | null; stageId?: string | null; subLevelId?: string | null }

export interface SchoolDetails {
  schoolId: string;
  schoolName: string;
  schoolAddress?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  county?: string | null;
  zipCode?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  description?: string | null;
}

interface StoredAuth {
  token: string;
  userId: string;
  schoolId: string | null;
  roles: string[];
  username: string;
  // convenience UI fields populated from auth or school GET
  schoolName?: string | null;
  schoolLogoUrl?: string | null;
  schoolAddress?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  county?: string | null;
  zipCode?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  description?: string | null;
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

  getSubjects(schoolId: string): Observable<Array<{ subjectId: string; name: string; stage: number; stageId?: string | null; description?: string | null; subLevelId?: string | null }>> {
    // include optional subLevelId when provided by backend
    return this.process(
      this.http
        .get<ApiResponse<Array<{ subjectId: string; name: string; stage: number; stageId?: string | null; description?: string | null }>>>(`${this.base}/schools/${schoolId}/subjects`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // Topics
  getTopics(schoolId: string, subjectId: string): Observable<Array<{ topicId: string; subtopic: string; notes?: string | null; name?: string | null; parentTopicId?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<any>>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics`, this.headers())
        .pipe(map(res => {
          const raw = this.unwrap(res) ?? [];
          return (raw as Array<any>).map(item => ({
            topicId: item.topicId,
            subtopic: item.subtopic ?? item.name ?? item.TopicName ?? item.topicName ?? '',
            notes: item.notes ?? null,
            name: item.name ?? item.TopicName ?? item.topicName ?? null,
            parentTopicId: item.parentTopicId ?? null
          }));
        }))
    );
  }

  getTopic(topicId: string): Observable<{ topicId: string; notes?: string | null; name?: string | null; parentTopicId?: string | null; subtopics?: Array<{ subTopicId?: string; name?: string | null; notes?: string | null }> }> {
    return this.process(
      this.http
        .get<ApiResponse<any>>(`${this.base}/topics/${topicId}`, this.headers())
        .pipe(map(res => {
          const raw = this.unwrap(res) ?? {};
          const subtopicsRaw = (raw.subtopics ?? []) as Array<any>;
          const subtopics = subtopicsRaw.map((st, idx) => ({
            subTopicId: st.subTopicId ?? st.subTopicId ?? undefined,
            name: st.name ?? st.subtopic ?? null,
            notes: st.notes ?? null
          }));
          return {
            topicId: raw.topicId,
            notes: raw.notes ?? null,
            name: raw.name ?? raw.TopicName ?? raw.topicName ?? null,
            parentTopicId: raw.parentTopicId ?? null,
            subtopics
          };
        }))
    );
  }

  createTopic(schoolId: string, subjectId: string, payload: { subtopic?: string; Subtopic?: string; notes?: string | null; Notes?: string | null; TopicName?: string | null; ParentTopicId?: string | null; clientCorrelationId?: string | null; stageId?: string | null }): Observable<{ topicId: string; storyId?: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ topicId: string; storyId?: string }>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateTopic(schoolId: string, subjectId: string, topicId: string, payload: { subtopic?: string; Subtopic?: string; notes?: string | null; Notes?: string | null; TopicName?: string | null; ParentTopicId?: string | null }): Observable<void> {
    return this.process(
      this.http
        .put<ApiResponse<null>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics/${topicId}`, payload, this.headers())
        .pipe(map(() => {}))
    );
  }

  deleteTopic(schoolId: string, subjectId: string, topicId: string): Observable<{ deletedIds?: string[] }> {
    return this.process(
      this.http
        .delete<ApiResponse<{ deletedIds?: string[] }>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics/${topicId}`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // Soft-delete all topics for a school (head teacher only)
  clearTopicsForSchool(schoolId: string): Observable<{ count: number }> {
    return this.process(
      this.http
        .post<ApiResponse<{ count: number }>>(`${this.base}/schools/${schoolId}/topics/clear`, null, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // Stories
  getStories(schoolId: string, subjectId: string): Observable<Array<StorySummary>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<StorySummary>>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/stories`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createStory(schoolId: string, subjectId: string, payload: CreateStoryRequest): Observable<{ storyId: string; createdAt: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ storyId: string; createdAt: string }>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/stories`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateStory(schoolId: string, subjectId: string, storyId: string, payload: { topicId?: string | null; subTopicId?: string | null; subTopicName?: string | null; user?: string | null; payload?: string | null }): Observable<void> {
    return this.process(
      this.http
        .put<ApiResponse<null>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/stories/${storyId}`, payload, this.headers())
        .pipe(map(() => {}))
    );
  }

  // stages
  getStages(schoolId: string): Observable<Array<StageDetails>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<StageDetails>>>(`${this.base}/schools/${schoolId}/stages`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getMembership(schoolId: string, userId: string): Observable<MembershipDetails> {
    return this.process(
      this.http
        .get<ApiResponse<MembershipDetails>>(`${this.base}/schools/${schoolId}/memberships/${userId}`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createStage(schoolId: string, payload: CreateStageRequest): Observable<StageDetails> {
    return this.process(
      this.http
        .post<ApiResponse<StageDetails>>(`${this.base}/schools/${schoolId}/stages`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateStage(schoolId: string, stageId: string, payload: CreateStageRequest): Observable<StageDetails> {
    return this.process(
      this.http
        .put<ApiResponse<StageDetails>>(`${this.base}/schools/${schoolId}/stages/${stageId}`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  deleteStage(schoolId: string, stageId: string): Observable<void> {
    return this.process(
      this.http
        .delete<ApiResponse<null>>(`${this.base}/schools/${schoolId}/stages/${stageId}`, this.headers())
        .pipe(map(() => {}))
    );
  }

  getTeacherSubjects(schoolId: string, teacherId: string): Observable<Array<{ subjectId: string; name: string; stage: number; description?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ subjectId: string; name: string; stage: number; description?: string | null }>>>(`${this.base}/schools/${schoolId}/teachers/${teacherId}/subjects`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getStudents(schoolId: string): Observable<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null; stageId?: string | null; level?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null }>>>(`${this.base}/schools/${schoolId}/students`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // learners endpoint is accessible to head teachers and teachers in the school and returns full student list
  getLearners(schoolId: string): Observable<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null; stageId?: string | null; subLevelId?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null; stageId?: string | null; subLevelId?: string | null }>>>(`${this.base}/schools/${schoolId}/learners`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getStudentsForStage(schoolId: string, stageId: string): Observable<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null; stageId?: string | null; subLevelId?: string | null }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ userId: string; username: string; firstName?: string | null; lastName?: string | null; stageId?: string | null; subLevelId?: string | null }>>>(`${this.base}/schools/${schoolId}/students?stageId=${encodeURIComponent(stageId)}`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateStudent(schoolId: string, userId: string, payload: { firstName?: string | null; lastName?: string | null; username?: string | null; stageId?: string | null; subLevelId?: string | null }): Observable<void> {
    return this.process(
      this.http
        .put<ApiResponse<null>>(`${this.base}/schools/${schoolId}/students/${userId}`, payload, this.headers())
        .pipe(map(() => {}))
    );
  }

  // Sublevels
  getSubLevels(schoolId: string, stageId: string): Observable<Array<SubLevelDetails>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<SubLevelDetails>>>(`${this.base}/schools/${schoolId}/stages/${stageId}/sublevels`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  createSubLevel(schoolId: string, stageId: string, payload: CreateSubLevelRequest): Observable<SubLevelDetails> {
    return this.process(
      this.http
        .post<ApiResponse<SubLevelDetails>>(`${this.base}/schools/${schoolId}/stages/${stageId}/sublevels`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  // Subtopics (child items under a Topic)
  createSubTopic(schoolId: string, subjectId: string, topicId: string, payload: { Name: string; Notes?: string | null }): Observable<{ subTopicId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ subTopicId: string }>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics/${topicId}/subtopics`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateSubTopic(schoolId: string, subjectId: string, topicId: string, subTopicId: string, payload: { Name?: string | null; Notes?: string | null }): Observable<void> {
    return this.process(
      this.http
        .put<ApiResponse<null>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics/${topicId}/subtopics/${subTopicId}`, payload, this.headers())
        .pipe(map(() => {}))
    );
  }

  deleteSubTopic(schoolId: string, subjectId: string, topicId: string, subTopicId: string): Observable<void> {
    return this.process(
      this.http
        .delete<ApiResponse<null>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/topics/${topicId}/subtopics/${subTopicId}`, this.headers())
        .pipe(map(() => {}))
    );
  }
  updateSubLevel(schoolId: string, stageId: string, subLevelId: string, payload: CreateSubLevelRequest): Observable<SubLevelDetails> {
    return this.process(
      this.http
        .put<ApiResponse<SubLevelDetails>>(`${this.base}/schools/${schoolId}/stages/${stageId}/sublevels/${subLevelId}`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  deleteSubLevel(schoolId: string, stageId: string, subLevelId: string): Observable<void> {
    return this.process(
      this.http
        .delete<ApiResponse<null>>(`${this.base}/schools/${schoolId}/stages/${stageId}/sublevels/${subLevelId}`, this.headers())
        .pipe(map(() => {}))
    );
  }

  deleteStudent(schoolId: string, userId: string): Observable<void> {
    return this.process(
      this.http
        .delete<ApiResponse<null>>(`${this.base}/schools/${schoolId}/students/${userId}`, this.headers())
        .pipe(map(() => {}))
    );
  }

  createCourseForSubject(schoolId: string, subjectId: string, payload: CreateCourseRequest): Observable<{ courseId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ courseId: string }>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/courses`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getCoursesForSubject(schoolId: string, subjectId: string): Observable<Array<{ courseId: string; title: string; summary?: string | null; level?: string | null; isPublished: boolean }>> {
    return this.process(
      this.http
        .get<ApiResponse<Array<{ courseId: string; title: string; summary?: string | null; level?: string | null; isPublished: boolean }>>>(`${this.base}/schools/${schoolId}/subjects/${subjectId}/courses`, this.headers())
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

  deleteTeacher(schoolId: string, teacherId: string): Observable<{ userId: string; isDeleted: boolean; deletedAt?: string | null }> {
    return this.process(
      this.http
        .delete<ApiResponse<{ userId: string; isDeleted: boolean; deletedAt?: string | null }>>(`${this.base}/schools/${schoolId}/teachers/${teacherId}`, this.headers())
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

  createSchool(payload: CreateSchoolRequest): Observable<{ schoolId: string }> {
    return this.process(
      this.http
        .post<ApiResponse<{ schoolId: string }>>(`${this.base}/schools`, payload, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  getSchool(schoolId: string): Observable<SchoolDetails> {
    return this.process(
      this.http
        .get<ApiResponse<SchoolDetails>>(`${this.base}/schools/${schoolId}`, this.headers())
        .pipe(map(res => this.unwrap(res)))
    );
  }

  updateSchool(schoolId: string, payload: CreateSchoolRequest): Observable<{ schoolId: string }> {
    return this.process(
      this.http
        .put<ApiResponse<{ schoolId: string }>>(`${this.base}/schools/${schoolId}`, payload, this.headers())
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
    const schoolName = anyRes.schoolName ?? anyRes.SchoolName ?? anyRes.data?.schoolName ?? anyRes.data?.SchoolName ?? null;
    let roles: any = anyRes.roles ?? anyRes.Roles ?? anyRes.data?.roles ?? anyRes.data?.Roles ?? [];
    // normalize roles to a string[] regardless of server shape (string or array)
    if (!roles) roles = [];
    if (typeof roles === 'string') roles = [roles];
    if (!Array.isArray(roles)) roles = [];
    const username = anyRes.username ?? anyRes.Username ?? anyRes.data?.username ?? anyRes.data?.Username ?? '';

    // Ensure schoolId is stored as a stable string (if provided) to avoid type/coercion issues.
    const normalizedSchoolId = schoolId ? String(schoolId) : null;

    const toStore: StoredAuth = {
      token: token as string,
      userId: userId as string,
      schoolId: normalizedSchoolId,
      roles: (roles as string[]) ?? [],
      username: username as string,
      schoolName: (schoolName as string) ?? null,
      // additional fields may be attached by other flows; default to null
      schoolLogoUrl: null,
      schoolAddress: null,
      city: null,
      state: null,
      country: null,
      county: null,
      zipCode: null,
      phoneNumber: null,
      email: null,
      description: null,
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
      // Debug: log whether a token is present when building headers
      try {
        console.debug('ApiService.headers: includeAuth=', includeAuth, 'tokenPresent=', !!auth?.token, auth && auth.token ? (`${String(auth.token).slice(0,10)}...`) : null);
      } catch {}
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
