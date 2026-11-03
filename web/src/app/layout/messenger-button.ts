import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SiteService } from '../core/site.service';
import { Icon } from '../shared/icon';

@Component({
  selector: 'app-messenger-button',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host { display: block; }
    a {
      position: fixed; bottom: 1.2rem; right: 1.2rem; z-index: 40;
      width: 56px; height: 56px; border-radius: 50%;
      background: #1877f2;
      color: white; display: flex; align-items: center; justify-content: center;
      box-shadow: 0 4px 12px rgba(24,119,242,.4);
      text-decoration: none;
    }
  `,
  template: `
    @if (siteService.site().facebookUrl) {
      <a [href]="siteService.site().facebookUrl" target="_blank" rel="noopener" aria-label="Sumlers Construction on Facebook">
        <app-icon name="facebook" />
      </a>
    }
  `,
})
export class MessengerButton {
  protected readonly siteService = inject(SiteService);
}
