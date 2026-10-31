import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/admin.service';
import { Icon } from '../../shared/icon';
import { PromotionsEditor } from './promotions-editor';
import { SiteEditor } from './site-editor';

type Tab = 'promotions' | 'site';

@Component({
  selector: 'app-admin',
  imports: [FormsModule, Icon, PromotionsEditor, SiteEditor],
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
