import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/admin.service';
import { AlertSettings, EmailSettings, SettingsView } from '../../core/models';
import { Icon } from '../../shared/icon';
import { AdminMessage, describeError } from './admin-errors';

/** The form keeps address lists as one-address-per-line text. */
interface SettingsDraft {
  email: EmailSettings;
  alerts: AlertSettings;
  notifyText: string;
  recipientsText: string;
}

@Component({
  selector: 'app-settings-tab',
  imports: [FormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings-tab.html',
  styleUrls: ['./admin-shared.scss', './site-editor.scss', './settings-tab.scss'],
})
export class SettingsTab {
  private readonly admin = inject(AdminService);

  protected readonly draft = signal<SettingsDraft | null>(null);
  protected readonly hasPassword = signal(false);
  protected readonly dirty = signal(false);
  protected readonly busy = signal(false);
  protected readonly message = signal<AdminMessage>(null);
  protected readonly healthUrl = `${location.origin}/health`;

  constructor() {
    this.admin.getSettings().subscribe({
      next: (view) => this.accept(view),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected changed(): void {
    this.dirty.set(true);
  }

  protected save(): void {
    const draft = this.draft();
    if (!draft) return;
    this.busy.set(true);
    const email = { ...draft.email, port: Number(draft.email.port) || 587, notifyAddresses: lines(draft.notifyText) };
    const alerts = { ...draft.alerts, recipients: lines(draft.recipientsText) };
    this.admin.saveSettings({ email, alerts }).subscribe({
      next: (view) => {
        this.accept(view);
        this.message.set({ kind: 'ok', text: 'Settings saved. Use "Send test email" to check the mail server.' });
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected sendTest(): void {
    this.busy.set(true);
    this.admin.sendTestEmail().subscribe({
      next: ({ message }) => {
        this.busy.set(false);
        this.message.set({ kind: 'ok', text: message });
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private accept(view: SettingsView): void {
    this.hasPassword.set(view.hasPassword);
    this.draft.set({
      email: { ...view.email, password: '' },
      alerts: view.alerts,
      notifyText: view.email.notifyAddresses.join('\n'),
      recipientsText: view.alerts.recipients.join('\n'),
    });
    this.dirty.set(false);
    this.busy.set(false);
  }

  private fail(err: HttpErrorResponse): void {
    this.busy.set(false);
    if (err.status === 401) this.admin.signOut();
    this.message.set({ kind: 'error', text: describeError(err) });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}

function lines(text: string): string[] {
  return text
    .split(/[\n,;]/)
    .map((s) => s.trim())
    .filter(Boolean);
}
