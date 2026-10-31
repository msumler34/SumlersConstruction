import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { Promotion, SiteInfo } from './models';

const KEY_STORAGE = 'sumlers-admin-key';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  /** Kept in sessionStorage only, so closing the browser signs the admin out. */
  readonly key = signal<string>(readStoredKey());

  verify(key: string): Observable<void> {
    return this.http.get<void>('/api/admin/verify', { headers: this.headers(key) });
  }

  signIn(key: string): void {
    this.key.set(key);
    trySessionStorage((s) => s.setItem(KEY_STORAGE, key));
  }

  signOut(): void {
    this.key.set('');
    trySessionStorage((s) => s.removeItem(KEY_STORAGE));
  }

  list(): Observable<Promotion[]> {
    return this.http.get<Promotion[]>('/api/admin/promotions', { headers: this.headers() });
  }

  save(promotion: Promotion): Observable<Promotion> {
    const headers = this.headers();
    return promotion.id
      ? this.http.put<Promotion>(`/api/admin/promotions/${encodeURIComponent(promotion.id)}`, promotion, { headers })
      : this.http.post<Promotion>('/api/admin/promotions', promotion, { headers });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/promotions/${encodeURIComponent(id)}`, { headers: this.headers() });
  }

  getSite(): Observable<SiteInfo> {
    return this.http.get<SiteInfo>('/api/site');
  }

  saveSite(site: SiteInfo): Observable<SiteInfo> {
    return this.http.put<SiteInfo>('/api/admin/site', site, { headers: this.headers() });
  }

  upload(file: File): Observable<{ url: string }> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<{ url: string }>('/api/admin/uploads', body, { headers: this.headers() });
  }

  private headers(key = this.key()): HttpHeaders {
    return new HttpHeaders({ 'X-Admin-Key': key });
  }
}

function readStoredKey(): string {
  let value = '';
  trySessionStorage((s) => (value = s.getItem(KEY_STORAGE) ?? ''));
  return value;
}

function trySessionStorage(action: (storage: Storage) => void): void {
  try {
    action(sessionStorage);
  } catch {
    // Storage can be blocked (private mode). The admin then signs in on each visit.
  }
}
