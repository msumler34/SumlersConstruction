import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { Icon } from '../../shared/icon';
import { Reveal } from '../../shared/reveal';
import { SrcsetPipe } from '../../shared/srcset';
import { NotFound } from '../not-found';
import { PageHero } from '../page-hero';

/** A service's own page, such as /deck-construction. Everything on it is edited in the admin page. */
@Component({
  selector: 'app-service-page',
  imports: [RouterLink, Icon, Reveal, PageHero, NotFound, SrcsetPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './service-page.html',
  styleUrl: './service-page.scss',
})
export class ServicePageComponent {
  /** From the address, through the router's input binding. */
  readonly serviceKey = input('');

  protected readonly siteService = inject(SiteService);
  private readonly router = inject(Router);

  private readonly key = computed(() => this.serviceKey().toLowerCase());
  protected readonly service = computed(() => this.siteService.site().services.find((s) => s.key === this.key()));
  protected readonly otherServices = computed(() => this.siteService.site().services.filter((s) => s.key !== this.key()));

  constructor() {
    // An old address of a renamed page moves to the current one. The server does the same for fresh visits.
    effect(() => {
      const key = this.key();
      const moved = this.siteService.site().services.find((s) => s.previousKeys.includes(key));
      if (moved && !this.service()) void this.router.navigate(['/', moved.key], { replaceUrl: true });
    });
  }

  /** Splits text into paragraphs at blank lines. */
  protected paragraphs(text: string): string[] {
    return text
      .split(/\n\s*\n/)
      .map((p) => p.trim())
      .filter(Boolean);
  }
}
