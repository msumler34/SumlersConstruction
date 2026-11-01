import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { catchError, filter, map, of, skip, switchMap } from 'rxjs';
import { PageMeta } from './models';
import { pageData } from './site.service';

/**
 * Keeps the tab title, description and Google tags right as visitors move between pages.
 * The server already writes them into each page's HTML; this asks it for the same values,
 * so the two always agree.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  start(): void {
    // The server wrote the first page's tags into its HTML, so only later pages need asking for.
    const firstPageDone = pageData(this.document) ? 1 : 0;
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        skip(firstPageDone),
        map((e) => e.urlAfterRedirects.split(/[?#]/)[0]),
        switchMap((path) => this.http.get<PageMeta>('/api/page-meta', { params: { path } }).pipe(catchError(() => of(null)))),
      )
      .subscribe((page) => page && this.apply(page));
  }

  private apply(page: PageMeta): void {
    this.title.setTitle(page.title);
    this.setTag('name', 'description', page.description);
    this.setTag('name', 'robots', page.noIndex ? 'noindex' : '');
    this.setTag('property', 'og:title', page.title);
    this.setTag('property', 'og:description', page.description);
    this.setTag('property', 'og:url', page.canonicalUrl);
    this.setTag('property', 'og:image', page.imageUrl);
    this.setCanonical(page.canonicalUrl);
  }

  private setTag(attribute: 'name' | 'property', key: string, content: string): void {
    const selector = `${attribute}="${key}"`;
    if (content) {
      this.meta.updateTag({ [attribute]: key, content }, selector);
    } else {
      this.meta.removeTag(selector);
    }
  }

  private setCanonical(url: string): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!url) {
      link?.remove();
      return;
    }
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'canonical';
      this.document.head.appendChild(link);
    }
    link.href = url;
  }
}
