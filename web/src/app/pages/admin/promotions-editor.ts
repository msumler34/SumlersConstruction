import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../../core/admin.service';
import { Promotion } from '../../core/models';
import { Icon } from '../../shared/icon';
import { ImageField } from '../../shared/image-field';
import { PromoCard } from '../../shared/promo-card';
import { AdminMessage, describeError } from './admin-errors';

@Component({
  selector: 'app-promotions-editor',
  imports: [ReactiveFormsModule, Icon, ImageField, PromoCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './promotions-editor.html',
  styleUrls: ['./admin-shared.scss', './promotions-editor.scss'],
})
export class PromotionsEditor {
  private readonly admin = inject(AdminService);

  protected readonly promotions = signal<Promotion[]>([]);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly message = signal<AdminMessage>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    id: [''],
    title: ['', [Validators.required, Validators.maxLength(120)]],
    summary: ['', Validators.maxLength(600)],
    badge: [''],
    imageUrl: [''],
    ctaText: [''],
    ctaUrl: [''],
    isFeatured: [false],
    isActive: [true],
    sortOrder: [0],
    startsOn: [''],
    endsOn: [''],
  });

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly preview = computed<Promotion>(() => {
    this.formValue();
    return toPromotion(this.form.getRawValue());
  });

  constructor() {
    this.load();
  }

  protected newPromotion(): void {
    this.form.reset({ isActive: true, sortOrder: this.promotions().length });
    this.editingId.set('');
    this.message.set(null);
  }

  protected edit(promotion: Promotion): void {
    this.form.reset({
      ...promotion,
      badge: promotion.badge ?? '',
      imageUrl: promotion.imageUrl ?? '',
      ctaText: promotion.ctaText ?? '',
      ctaUrl: promotion.ctaUrl ?? '',
      startsOn: promotion.startsOn ?? '',
      endsOn: promotion.endsOn ?? '',
    });
    this.editingId.set(promotion.id);
    this.message.set(null);
  }

  protected cancel(): void {
    this.editingId.set(null);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.admin.save(toPromotion(this.form.getRawValue())).subscribe({
      next: () => {
        this.editingId.set(null);
        this.message.set({ kind: 'ok', text: 'Saved. The landing page shows the change right away.' });
        this.load();
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected toggleActive(promotion: Promotion): void {
    this.busy.set(true);
    this.admin.save({ ...promotion, isActive: !promotion.isActive }).subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected remove(promotion: Promotion): void {
    if (!confirm(`Delete "${promotion.title}"? This cannot be undone.`)) return;
    this.busy.set(true);
    this.admin.delete(promotion.id).subscribe({
      next: () => {
        if (this.editingId() === promotion.id) this.editingId.set(null);
        this.message.set({ kind: 'ok', text: 'Deleted.' });
        this.load();
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected setImage(url: string): void {
    this.form.controls.imageUrl.setValue(url);
  }

  protected status(p: Promotion): string {
    if (!p.isActive) return 'Hidden';
    const today = new Date().toISOString().slice(0, 10);
    if (p.startsOn && p.startsOn > today) return 'Scheduled';
    if (p.endsOn && p.endsOn < today) return 'Expired';
    return 'Live';
  }

  private load(): void {
    this.busy.set(true);
    this.admin.list().subscribe({
      next: (list) => {
        this.promotions.set(list);
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

/** The editor form keeps optional text as '' rather than null, so inputs bind cleanly. */
type PromotionForm = Omit<Promotion, 'badge' | 'imageUrl' | 'ctaText' | 'ctaUrl' | 'startsOn' | 'endsOn'> &
  Record<'badge' | 'imageUrl' | 'ctaText' | 'ctaUrl' | 'startsOn' | 'endsOn', string>;

function toPromotion(value: PromotionForm): Promotion {
  const orNull = (s: string) => (s.trim() ? s.trim() : null);
  return {
    ...value,
    title: value.title.trim(),
    summary: value.summary.trim(),
    badge: orNull(value.badge),
    imageUrl: orNull(value.imageUrl),
    ctaText: orNull(value.ctaText),
    ctaUrl: orNull(value.ctaUrl),
    sortOrder: Number(value.sortOrder) || 0,
    startsOn: orNull(value.startsOn),
    endsOn: orNull(value.endsOn),
  };
}
