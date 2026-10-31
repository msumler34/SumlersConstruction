import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { scrollToFragmentWhenReady } from '../../shared/fragment-scroll';
import { Icon } from '../../shared/icon';
import { Reveal } from '../../shared/reveal';
import { PageHero } from '../page-hero';

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
    scrollToFragmentWhenReady(() => this.siteService.site().services.length > 0);
  }

  protected readonly heroImage =
    'https://images.unsplash.com/photo-1772442198624-4fc4d7281e89?auto=format&fit=crop&w=2000&q=80';

  protected readonly alsoHelp = [
    'Pergolas & gazebos',
    'Fences & gates',
    'Windows & doors',
    'Siding & trim',
    'Flooring',
    'Drywall & paint',
    'Basement finishing',
    'Accessibility ramps',
    'Sheds & storage',
    'Rot & water damage',
    'Stairs & railings',
    'Punch lists',
  ];
}
