import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { scrollToFragmentWhenReady } from '../../shared/fragment-scroll';
import { Icon } from '../../shared/icon';
import { Reveal } from '../../shared/reveal';
import { PageHero } from '../page-hero';

/** The /services overview. Its text is edited in the admin page; each service links to its own page. */
@Component({
  selector: 'app-services',
  imports: [RouterLink, Icon, Reveal, PageHero],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './services.html',
  styleUrl: './services.scss',
})
export class Services {
  protected readonly siteService = inject(SiteService);

  constructor() {
    // Old links such as /services#decks still scroll to that service.
    scrollToFragmentWhenReady(() => this.siteService.site().services.length > 0);
  }
}
