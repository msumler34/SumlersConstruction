import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Promotion, ServiceOffering, ServicePage, ServicesPageText, SiteInfo } from './models';

export const EMPTY_SERVICE_PAGE: ServicePage = {
  heading: '',
  intro: '',
  sections: [],
  gallery: [],
  faqs: [],
  seoTitle: '',
  seoDescription: '',
};

export const EMPTY_SERVICES_PAGE: ServicesPageText = {
  eyebrow: '',
  title: '',
  text: '',
  imageUrl: '',
  moreTitle: '',
  moreText: '',
  moreItems: [],
  seoTitle: '',
  seoDescription: '',
};

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
  hero: { eyebrow: '', title: '', highlight: '', text: '' },
  whyUs: { title: '', text: '', imageUrl: '', items: [] },
  process: { title: '', text: '', imageUrl: '', items: [] },
  seo: { title: '', description: '', siteUrl: '' },
  servicesPage: EMPTY_SERVICES_PAGE,
};

@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly http = inject(HttpClient);

  private readonly siteState = signal<SiteInfo>(FALLBACK_SITE);
  readonly site = this.siteState.asReadonly();

  /** True once /api/site has answered (or failed), so pages can tell "not loaded yet" from "not found". */
  private readonly loadedState = signal(false);
  readonly loaded = this.loadedState.asReadonly();

  private readonly promotions = signal<Promotion[] | undefined>(undefined);

  readonly promotionsLoaded = computed(() => this.promotions() !== undefined);
  readonly featuredPromotion = computed(() => this.promotions()?.find((p) => p.isFeatured) ?? null);
  readonly otherPromotions = computed(() => {
    const featured = this.featuredPromotion();
    return (this.promotions() ?? []).filter((p) => p !== featured);
  });

  readonly phoneHref = computed(() => 'tel:' + this.site().phone.replace(/[^\d+]/g, ''));
  readonly emailHref = computed(() => 'mailto:' + this.site().email);

  constructor() {
    const data = pageData(inject(DOCUMENT));
    if (data) {
      this.setSite(withPageDefaults({ ...FALLBACK_SITE, ...data.site }));
      this.promotions.set(data.promotions);
      return;
    }
    this.reload();
    this.http.get<Promotion[]>('/api/promotions').subscribe({
      next: (promotions) => this.promotions.set(promotions),
      error: () => this.promotions.set([]),
    });
  }

  /** Fetches the site details again, for example after the admin saves Business Info. */
  reload(): void {
    this.http.get<SiteInfo>('/api/site').subscribe({
      next: (site) => this.setSite(withPageDefaults({ ...FALLBACK_SITE, ...site })),
      error: () => this.setSite(FALLBACK_SITE),
    });
  }

  private setSite(site: SiteInfo): void {
    this.siteState.set(site);
    this.loadedState.set(true);
  }
}

/**
 * The site details and specials the server wrote into the page (PageData.cs), so the first view needs no API call.
 * Null when the page came from the Angular dev server, which does not write them.
 */
export function pageData(document: Document): { site: SiteInfo; promotions: Promotion[] } | null {
  const json = document.getElementById('page-data')?.textContent;
  if (!json) return null;
  try {
    return JSON.parse(json);
  } catch {
    return null;
  }
}

/** Fills the page text that a site saved before service pages existed does not have. */
export function withPageDefaults(site: SiteInfo): SiteInfo {
  return {
    ...site,
    seo: { ...FALLBACK_SITE.seo, ...site.seo },
    servicesPage: { ...EMPTY_SERVICES_PAGE, ...site.servicesPage },
    services: (site.services ?? []).map(withServiceDefaults),
  };
}

export function withServiceDefaults(service: ServiceOffering): ServiceOffering {
  return {
    ...service,
    highlights: service.highlights ?? [],
    previousKeys: service.previousKeys ?? [],
    page: { ...EMPTY_SERVICE_PAGE, ...service.page },
  };
}
