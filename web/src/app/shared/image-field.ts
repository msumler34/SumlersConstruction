import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, model, signal } from '@angular/core';
import { AdminService } from '../core/admin.service';
import { Icon } from './icon';

/** Image URL box with an Upload button and a thumbnail. Uploads go to /uploads on the server. */
@Component({
  selector: 'app-image-field',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: grid;
      gap: 6px;
    }
    .row {
      display: flex;
      gap: 8px;
      align-items: center;
    }
    img,
    .empty {
      width: 64px;
      height: 46px;
      flex-shrink: 0;
      border-radius: 8px;
      object-fit: cover;
      background: var(--sand);
    }
    input[type='text'] {
      flex: 1;
      min-width: 0;
      padding: 11px 13px;
      border-radius: var(--radius-sm);
      border: 1.5px solid var(--line);
      font: inherit;
      font-size: 0.92rem;
    }
    input[type='text']:focus {
      outline: none;
      border-color: var(--amber);
    }
    .upload {
      cursor: pointer;
      font-size: 0.85rem;
    }
    .error {
      font-size: 0.82rem;
      color: #b3261e;
    }
  `,
  template: `
    <div class="row">
      @if (value()) {
        <img [src]="value()" alt="" />
      } @else {
        <span class="empty"></span>
      }
      <input type="text" [id]="inputId()" [value]="value()" (input)="value.set($any($event.target).value)"
        placeholder="https://… or upload a photo" />
      <label class="btn btn--outline btn--sm upload">
        <app-icon name="upload" /> {{ uploading() ? 'Uploading…' : 'Upload' }}
        <input type="file" accept="image/jpeg,image/png,image/webp" (change)="upload($event)" hidden />
      </label>
    </div>
    @if (error()) {
      <span class="error">{{ error() }}</span>
    }
  `,
})
export class ImageField {
  readonly value = model('');
  readonly inputId = input<string>();

  private readonly admin = inject(AdminService);
  protected readonly uploading = signal(false);
  protected readonly error = signal('');

  protected upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.uploading.set(true);
    this.error.set('');
    this.admin.upload(file).subscribe({
      next: ({ url }) => {
        this.value.set(url);
        this.uploading.set(false);
        input.value = '';
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.error ?? 'Upload failed. Please try again.');
        this.uploading.set(false);
      },
    });
  }
}
