import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/admin.service';
import { SiteInfo } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { Icon } from '../../shared/icon';
import { ImageField } from '../../shared/image-field';
import { AdminMessage, describeError } from './admin-errors';

/** Edits everything in /api/site: contact details, social links, photos, services, gallery and reviews. */
@Component({
  selector: 'app-site-editor',
  imports: [FormsModule, Icon, ImageField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './site-editor.html',
  styleUrls: ['./admin-shared.scss', './site-editor.scss'],
})
export class SiteEditor {
  private readonly admin = inject(AdminService);
  private readonly siteService = inject(SiteService);

  protected readonly serviceIcons = ['deck', 'home', 'porch', 'tools', 'hammer', 'ruler', 'shield', 'handshake'];

  /** Bound with ngModel and changed in place; `edits` ticks on every change so `dirty` recomputes. */
  protected readonly draft = signal<SiteInfo | null>(null);
  private readonly saved = signal('');
  private readonly edits = signal(0);
  protected readonly dirty = computed(() => {
    this.edits();
    const draft = this.draft();
    return draft !== null && JSON.stringify(draft) !== this.saved();
  });
  protected readonly busy = signal(false);
  protected readonly message = signal<AdminMessage>(null);

  constructor() {
    this.load();
  }

  protected changed(): void {
    this.edits.update((n) => n + 1);
  }

  protected add<T>(list: T[], item: T): void {
    list.push(item);
    this.changed();
  }

  protected remove<T>(list: T[], index: number): void {
    list.splice(index, 1);
    this.changed();
  }

  protected move<T>(list: T[], index: number, offset: number): void {
    const target = index + offset;
    if (target < 0 || target >= list.length) return;
    [list[index], list[target]] = [list[target], list[index]];
    this.changed();
  }

  protected highlightsText(highlights: string[]): string {
    return highlights.join('\n');
  }

  protected setHighlights(service: { highlights: string[] }, text: string): void {
    service.highlights = text.split('\n');
    this.changed();
  }

  protected discard(): void {
    this.draft.set(JSON.parse(this.saved()));
    this.message.set(null);
  }

  protected save(): void {
    const draft = this.draft();
    if (!draft) return;
    this.busy.set(true);
    this.admin.saveSite(clean(draft)).subscribe({
      next: (site) => {
        this.accept(site);
        this.siteService.reload();
        this.message.set({ kind: 'ok', text: 'Saved. The whole site now shows the new details.' });
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private load(): void {
    this.busy.set(true);
    this.admin.getSite().subscribe({
      next: (site) => this.accept(site),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private accept(site: SiteInfo): void {
    const copy = withDefaults(site);
    this.saved.set(JSON.stringify(copy));
    this.draft.set(structuredClone(copy));
    this.busy.set(false);
  }

  private fail(err: HttpErrorResponse): void {
    this.busy.set(false);
    if (err.status === 401) this.admin.signOut();
    this.message.set({ kind: 'error', text: describeError(err) });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}

function withDefaults(site: SiteInfo): SiteInfo {
  return {
    ...site,
    address: site.address ?? '',
    heroImageUrl: site.heroImageUrl ?? '',
    services: site.services ?? [],
    testimonials: site.testimonials ?? [],
    gallery: site.gallery ?? [],
  };
}

/** Trims text and drops blank highlight lines before saving. */
function clean(site: SiteInfo): SiteInfo {
  return {
    ...site,
    yearsInBusiness: Number(site.yearsInBusiness) || 0,
    services: site.services.map((s) => ({ ...s, highlights: s.highlights.map((h) => h.trim()).filter(Boolean) })),
  };
}
