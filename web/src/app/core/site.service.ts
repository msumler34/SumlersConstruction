import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { Promotion, SiteInfo } from './models';

/** Used until /api/site answers, and if it fails, so the page never renders empty. */
const FALLBACK_SITE: SiteInfo = {
  businessName: 'Sumlers Construction',
  tagline: 'Custom decks, remodels and everything your home needs.',
  phone: '',
  email: '',
  address: '',
  serviceArea: '',
  hours: '',
  facebookUrl: '',
  messengerUrl: '',
  yearsInBusiness: 0,
  heroImageUrl: '',
  services: [],
  testimonials: [],
  gallery: [],
};

@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly http = inject(HttpClient);

  private readonly siteState = signal<SiteInfo>(FALLBACK_SITE);
  readonly site = this.siteState.asReadonly();

  private readonly promotions = toSignal(
    this.http.get<Promotion[]>('/api/promotions').pipe(catchError(() => of([] as Promotion[]))),
  );

  readonly promotionsLoaded = computed(() => this.promotions() !== undefined);
  readonly featuredPromotion = computed(() => this.promotions()?.find((p) => p.isFeatured) ?? null);
  readonly otherPromotions = computed(() => {
    const featured = this.featuredPromotion();
    return (this.promotions() ?? []).filter((p) => p !== featured);
  });

  readonly phoneHref = computed(() => 'tel:' + this.site().phone.replace(/[^\d+]/g, ''));
  readonly emailHref = computed(() => 'mailto:' + this.site().email);

  constructor() {
    this.reload();
  }

  /** Fetches the site details again, for example after the admin saves Business Info. */
  reload(): void {
    this.http.get<SiteInfo>('/api/site').subscribe({
      next: (site) => this.siteState.set({ ...FALLBACK_SITE, ...site }),
      error: () => this.siteState.set(FALLBACK_SITE),
    });
  }
}
