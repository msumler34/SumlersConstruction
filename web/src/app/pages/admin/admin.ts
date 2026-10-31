import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/admin.service';
import { Icon } from '../../shared/icon';
import { BackupsTab } from './backups-tab';
import { MessagesTab } from './messages-tab';
import { PromotionsEditor } from './promotions-editor';
import { SettingsTab } from './settings-tab';
import { SiteEditor } from './site-editor';

type Tab = 'promotions' | 'site' | 'messages' | 'settings' | 'backups';

@Component({
  selector: 'app-admin',
  imports: [FormsModule, Icon, PromotionsEditor, SiteEditor, MessagesTab, SettingsTab, BackupsTab],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin.html',
  styleUrl: './admin.scss',
})
export class Admin {
  protected readonly admin = inject(AdminService);

  protected readonly keyInput = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly tab = signal<Tab>('promotions');
  protected readonly unread = signal(0);

  protected readonly tabs: { id: Tab; label: string; icon: string }[] = [
    { id: 'promotions', label: 'Promoted topics', icon: 'star' },
    { id: 'site', label: 'Business info', icon: 'home' },
    { id: 'messages', label: 'Messages', icon: 'mail' },
    { id: 'settings', label: 'Settings', icon: 'tools' },
    { id: 'backups', label: 'Backups', icon: 'shield' },
  ];

  constructor() {
    // Shows the unread badge on the Messages tab as soon as the admin signs in.
    effect(() => {
      if (!this.admin.key()) return;
      this.admin.messages().subscribe({ next: (list) => this.unread.set(list.filter((m) => !m.isRead).length) });
    });
  }

  protected signIn(): void {
    const key = this.keyInput().trim();
    if (!key) return;
    this.busy.set(true);
    this.admin.verify(key).subscribe({
      next: () => {
        this.admin.signIn(key);
        this.busy.set(false);
        this.error.set('');
      },
      error: () => {
        this.busy.set(false);
        this.error.set('That admin key was not accepted.');
      },
    });
  }
}
