import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { scrollToFragmentWhenReady } from '../../shared/fragment-scroll';
import { Icon } from '../../shared/icon';
import { PromoCard, splitLink } from '../../shared/promo-card';
import { Reveal } from '../../shared/reveal';

@Component({
  selector: 'app-home',
  imports: [RouterLink, Icon, PromoCard, Reveal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly siteService = inject(SiteService);
  private readonly title = inject(Title);

  protected readonly featuredLink = computed(() => splitLink(this.siteService.featuredPromotion()?.ctaUrl ?? null));

  constructor() {
    scrollToFragmentWhenReady(() => this.siteService.promotionsLoaded() && this.siteService.site().services.length > 0);

    // The browser tab and Google title for the home page come from Business info.
    effect(() => {
      const seoTitle = this.siteService.site().seo.title;
      if (seoTitle) this.title.setTitle(seoTitle);
    });
  }
}
