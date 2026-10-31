import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminService } from '../../core/admin.service';
import { BackupInfo } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { Icon } from '../../shared/icon';
import { AdminMessage, describeError } from './admin-errors';

const REASONS: Record<string, string> = {
  daily: 'Daily automatic',
  'before-change': 'Before an edit',
  'before-restore': 'Before a restore',
  manual: 'Made by you',
  weekly: 'Weekly (emailed)',
  uploaded: 'Uploaded file',
};

@Component({
  selector: 'app-backups-tab',
  imports: [DatePipe, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './backups-tab.html',
  styleUrls: ['./admin-shared.scss', './backups-tab.scss'],
})
export class BackupsTab {
  private readonly admin = inject(AdminService);
  private readonly siteService = inject(SiteService);

  protected readonly backups = signal<BackupInfo[]>([]);
  protected readonly busy = signal(false);
  protected readonly message = signal<AdminMessage>(null);

  constructor() {
    this.load();
  }

  protected reasonLabel(reason: string): string {
    return REASONS[reason] ?? reason;
  }

  protected size(bytes: number): string {
    return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  protected create(): void {
    this.run(this.admin.createBackup(), () => 'Backup created.');
  }

  protected download(backup: BackupInfo): void {
    this.admin.downloadBackup(backup.name).subscribe({
      next: (blob) => saveFile(blob, backup.name),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected restore(backup: BackupInfo, createdAt: string | null): void {
    const question = `Put the whole site back the way it was on ${createdAt}?\n\nContent, photos, messages and settings will match that backup. A backup of the site as it is now is saved first, so you can undo this.`;
    if (!confirm(question)) return;
    this.run(this.admin.restoreBackup(backup.name), (result) => {
      this.siteService.reload();
      return result.message;
    });
  }

  protected upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.run(this.admin.uploadBackup(file), () => 'Backup uploaded. It is at the top of the list; use Restore to apply it.');
  }

  private run<T>(request: Observable<T>, success: (result: T) => string): void {
    this.busy.set(true);
    this.message.set(null);
    request.subscribe({
      next: (result) => {
        this.message.set({ kind: 'ok', text: success(result) });
        this.load();
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private load(): void {
    this.admin.backups().subscribe({
      next: (list) => {
        this.backups.set(list);
        this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private fail(err: HttpErrorResponse): void {
    this.busy.set(false);
    if (err.status === 401) this.admin.signOut();
    this.message.set({ kind: 'error', text: describeError(err) });
  }
}

function saveFile(blob: Blob, name: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}
