import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { AlertSettings, BackupInfo, ContactMessage, EmailSettings, Promotion, SettingsView, SiteInfo } from './models';

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

  getSettings(): Observable<SettingsView> {
    return this.http.get<SettingsView>('/api/admin/settings', { headers: this.headers() });
  }

  saveSettings(settings: { email: EmailSettings; alerts: AlertSettings }): Observable<SettingsView> {
    return this.http.put<SettingsView>('/api/admin/settings', settings, { headers: this.headers() });
  }

  sendTestEmail(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/admin/settings/test-email', null, { headers: this.headers() });
  }

  messages(): Observable<ContactMessage[]> {
    return this.http.get<ContactMessage[]>('/api/admin/messages', { headers: this.headers() });
  }

  markRead(id: string, isRead: boolean): Observable<void> {
    const url = `/api/admin/messages/${encodeURIComponent(id)}/read?isRead=${isRead}`;
    return this.http.put<void>(url, null, { headers: this.headers() });
  }

  deleteMessage(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/messages/${encodeURIComponent(id)}`, { headers: this.headers() });
  }

  backups(): Observable<BackupInfo[]> {
    return this.http.get<BackupInfo[]>('/api/admin/backups', { headers: this.headers() });
  }

  createBackup(): Observable<BackupInfo> {
    return this.http.post<BackupInfo>('/api/admin/backups', null, { headers: this.headers() });
  }

  downloadBackup(name: string): Observable<Blob> {
    return this.http.get(`/api/admin/backups/${encodeURIComponent(name)}`, { headers: this.headers(), responseType: 'blob' });
  }

  restoreBackup(name: string): Observable<{ message: string }> {
    const url = `/api/admin/backups/${encodeURIComponent(name)}/restore`;
    return this.http.post<{ message: string }>(url, null, { headers: this.headers() });
  }

  /** Sends the zip as the raw request body, which avoids the server's form size limits. */
  uploadBackup(file: File): Observable<BackupInfo> {
    const headers = this.headers().set('Content-Type', 'application/zip');
    return this.http.post<BackupInfo>('/api/admin/backups/upload', file, { headers });
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
