import { HttpErrorResponse } from '@angular/common/http';
import { Directive, ElementRef, Injector, afterNextRender, computed, inject, signal } from '@angular/core';
import { AdminService } from '../../core/admin.service';
import { SiteInfo } from '../../core/models';
import { SiteService, withPageDefaults } from '../../core/site.service';
import { AdminMessage, describeError } from './admin-errors';

/**
 * Shared by the admin tabs that edit /api/site (Business info and Service pages). Each tab loads the
 * whole site when it opens and saves the whole site, so switching tabs never loses a saved change.
 */
@Directive()
export abstract class SiteDraftEditor {
  protected readonly admin = inject(AdminService);
  private readonly siteService = inject(SiteService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

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

  /** Adds an item at the end of its list, then scrolls to it and puts the cursor in its first box. */
  protected add<T>(list: T[], item: T, listName: string): void {
    list.push(item);
    this.changed();
    afterNextRender(() => this.revealLastItem(listName), { injector: this.injector });
  }

  private revealLastItem(listName: string): void {
    // Only items of this list, not of a list nested inside one of them.
    const items = [...this.host.nativeElement.querySelectorAll<HTMLElement>(`[data-list="${listName}"] .item`)].filter(
      (item) => item.parentElement?.closest('[data-list]')?.getAttribute('data-list') === listName,
    );
    const last = items.at(-1);
    if (!last) return;
    if (last instanceof HTMLDetailsElement) last.open = true;
    last.scrollIntoView({ behavior: 'smooth', block: 'center' });
    last.querySelector<HTMLElement>('input:not([type=file]), textarea')?.focus({ preventScroll: true });
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

  /** Shows a list of short lines as one line each in a text box. */
  protected linesText(lines: string[]): string {
    return lines.join('\n');
  }

  protected setLines<T>(target: T, field: keyof T, text: string): void {
    target[field] = text.split('\n') as T[keyof T];
    this.changed();
  }

  protected discard(): void {
    this.draft.set(JSON.parse(this.saved()));
    this.afterLoad();
    this.message.set(null);
  }

  protected save(): void {
    const draft = this.draft();
    if (!draft) return;
    this.busy.set(true);
    this.admin.saveSite(this.beforeSave(clean(draft))).subscribe({
      next: (site) => {
        this.accept(site);
        this.siteService.reload();
        this.message.set({ kind: 'ok', text: this.savedMessage });
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  protected abstract readonly savedMessage: string;

  /** Lets a tab adjust the site just before it is sent. */
  protected beforeSave(site: SiteInfo): SiteInfo {
    return site;
  }

  /** Runs after the draft is loaded, saved or discarded. */
  protected afterLoad(): void {}

  private load(): void {
    this.busy.set(true);
    this.admin.getSite().subscribe({
      next: (site) => this.accept(site),
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private accept(site: SiteInfo): void {
    const copy = withEditorDefaults(site);
    this.saved.set(JSON.stringify(copy));
    this.draft.set(structuredClone(copy));
    this.afterLoad();
    this.busy.set(false);
  }

  private fail(err: HttpErrorResponse): void {
    this.busy.set(false);
    if (err.status === 401) this.admin.signOut();
    this.message.set({ kind: 'error', text: describeError(err) });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}

function withEditorDefaults(site: SiteInfo): SiteInfo {
  return withPageDefaults({
    ...site,
    address: site.address ?? '',
    heroImageUrl: site.heroImageUrl ?? '',
    testimonials: site.testimonials ?? [],
    gallery: site.gallery ?? [],
    hero: site.hero ?? { eyebrow: '', title: '', highlight: '', text: '' },
    whyUs: site.whyUs ?? { title: '', text: '', imageUrl: '', items: [] },
    process: site.process ?? { title: '', text: '', imageUrl: '', items: [] },
  });
}

/** Trims text and drops blank lines from the one-per-line lists before saving. */
function clean(site: SiteInfo): SiteInfo {
  const lines = (list: string[]) => list.map((h) => h.trim()).filter(Boolean);
  return {
    ...site,
    yearsInBusiness: Number(site.yearsInBusiness) || 0,
    seo: { ...site.seo, siteUrl: site.seo.siteUrl.trim().replace(/\/+$/, '') },
    servicesPage: { ...site.servicesPage, moreItems: lines(site.servicesPage.moreItems) },
    services: site.services.map((s) => ({ ...s, key: s.key.trim(), highlights: lines(s.highlights) })),
  };
}
