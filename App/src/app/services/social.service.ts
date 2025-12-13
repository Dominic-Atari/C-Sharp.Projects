import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

interface ApiError { message: string; details?: unknown }
interface ApiResponse<T> { data: T | null; error?: ApiError | null }

export interface CreatePostRequest { userId: string; caption: string; imageUrl?: string | null }
export interface PostDto { postId: string; userId: string; content?: string | null; imageUrl?: string | null; createdAt: string }

@Injectable({ providedIn: 'root' })
export class SocialService {
  private readonly base = `${environment.apiBase}/posts`;

  constructor(private http: HttpClient) {}

  private headers(): { headers?: HttpHeaders } {
    const headers: Record<string, string> = {};
    if (environment.apiKey) headers['x-api-key'] = environment.apiKey;
    return Object.keys(headers).length ? { headers: new HttpHeaders(headers) } : {};
  }

  private unwrap<T>(res: ApiResponse<T>): { data: T | null; error?: ApiError | null } {
    const anyRes = res as any;
    const data = res.data ?? anyRes?.Data ?? null;
    const error = res.error ?? anyRes?.Error ?? null;
    return { data, error };
  }

  private ensureUserId(): string {
    const userId = environment.defaultUserId?.trim();
    const guidPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
    if (!userId || !guidPattern.test(userId)) {
      throw new Error('defaultUserId must be set to a valid GUID that exists in the backend.');
    }
    return userId;
  }

  createPost(caption: string, imageUrl?: string | null): Observable<PostDto> {
    let userId: string;

    try {
      userId = this.ensureUserId();
    } catch (err) {
      return throwError(() => (err instanceof Error ? err : new Error('defaultUserId must be set.')));
    }

    const body: CreatePostRequest = {
      userId,
      caption: caption.trim(),
      imageUrl: imageUrl ?? null,
    };

    return this.http
      .post<ApiResponse<PostDto>>(this.base, body, this.headers())
      .pipe(
        map(res => {
          const { data, error } = this.unwrap(res);
          if (data) return data;
          throw new Error(error?.message ?? 'No post returned from API.');
        })
      );
  }

  getFeed(skip = 0, take = 50): Observable<PostDto[]> {
    const params = { skip, take } as any;
    return this.http
      .get<ApiResponse<PostDto[]>>(this.base, { ...this.headers(), params })
      .pipe(
        map(res => {
          const { data } = this.unwrap(res);
          return data ?? [];
        })
      );
  }
}
