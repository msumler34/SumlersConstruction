import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { scrollToFragmentWhenReady } from '../../shared/fragment-scroll';
import { Icon } from '../../shared/icon';
import { PromoCard, splitLink } from '../../shared/promo-card';
import { Reveal } from '../../shared/reveal';

const UNSPLASH = 'https://images.unsplash.com/photo-';
const photo = (id: string, width = 1200) => `${UNSPLASH}${id}?auto=format&fit=crop&w=${width}&q=80`;

@Component({
  selector: 'app-home',
  imports: [RouterLink, Icon, PromoCard, Reveal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly siteService = inject(SiteService);

  constructor() {
    scrollToFragmentWhenReady(() => this.siteService.promotionsLoaded() && this.siteService.site().services.length > 0);
  }

  protected readonly craftImage = photo('1773430272653-375174373c4f', 1400);

  protected readonly featuredLink = computed(() => splitLink(this.siteService.featuredPromotion()?.ctaUrl ?? null));

  protected readonly reasons = [
    { icon: 'shield', title: 'Licensed & insured', text: 'Permits pulled, inspections passed and coverage on every job.' },
    { icon: 'ruler', title: 'Clear, written quotes', text: 'You see every line item before work starts. No surprise invoices.' },
    { icon: 'handshake', title: 'One crew, start to finish', text: 'The people who quote your project are the people who build it.' },
    { icon: 'check', title: 'Clean job sites', text: 'We protect your home and leave it cleaner than we found it every day.' },
  ];

  protected readonly steps = [
    { title: 'Free consultation', text: 'We visit, listen to what you want and look at the space.' },
    { title: 'Design & quote', text: 'You get a clear plan, materials options and a written price.' },
    { title: 'Build', text: 'Our crew builds it right, with updates along the way.' },
    { title: 'Final walkthrough', text: 'We walk the finished work with you and fix anything you notice.' },
  ];
}
