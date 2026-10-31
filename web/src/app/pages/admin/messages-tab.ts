import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, output, signal } from '@angular/core';
import { AdminService } from '../../core/admin.service';
import { ContactMessage } from '../../core/models';
import { Icon } from '../../shared/icon';
import { AdminMessage, describeError } from './admin-errors';

/** Every contact form submission, kept even when the email about it failed. */
@Component({
  selector: 'app-messages-tab',
  imports: [DatePipe, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './messages-tab.html',
  styleUrls: ['./admin-shared.scss', './messages-tab.scss'],
})
export class MessagesTab {
  /** Tells the tab bar the new unread count. */
  readonly unreadChange = output<number>();

  private readonly admin = inject(AdminService);
  protected readonly messages = signal<ContactMessage[]>([]);
  protected readonly loaded = signal(false);
  protected readonly message = signal<AdminMessage>(null);
  protected readonly failedCount = computed(() => this.messages().filter((m) => m.emailStatus === 'Failed').length);

  constructor() {
    this.load();
  }

  protected toggleRead(item: ContactMessage): void {
    this.admin.markRead(item.id, !item.isRead).subscribe({
      next: () => this.replace({ ...item, isRead: !item.isRead }),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected remove(item: ContactMessage): void {
    if (!confirm(`Delete the message from ${item.name}? This cannot be undone.`)) return;
    this.admin.deleteMessage(item.id).subscribe({
      next: () => this.update(this.messages().filter((m) => m.id !== item.id)),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected replyLink(item: ContactMessage): string {
    const subject = encodeURIComponent(`Re: your ${item.service || 'project'} estimate request`);
    return `mailto:${item.email}?subject=${subject}`;
  }

  protected phoneLink(phone: string): string {
    return 'tel:' + phone.replace(/[^\d+]/g, '');
  }

  private load(): void {
    this.admin.messages().subscribe({
      next: (list) => {
        this.update(list);
        this.loaded.set(true);
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private replace(item: ContactMessage): void {
    this.update(this.messages().map((m) => (m.id === item.id ? item : m)));
  }

  private update(list: ContactMessage[]): void {
    this.messages.set(list);
    this.unreadChange.emit(list.filter((m) => !m.isRead).length);
  }

  private fail(err: HttpErrorResponse): void {
    if (err.status === 401) this.admin.signOut();
    this.message.set({ kind: 'error', text: describeError(err) });
  }
}
