import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { RouterOutlet } from '@angular/router';
import { SiteService } from './core/site.service';
import { Footer } from './layout/footer';
import { Header } from './layout/header';
import { MessengerButton } from './layout/messenger-button';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer, MessengerButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-header />
    <main>
      <router-outlet />
    </main>
    <app-footer />
    <app-messenger-button />
  `,
})
export class App {
  constructor() {
    const siteService = inject(SiteService);
    const meta = inject(Meta);

    // The Google description comes from Business info, so the admin can change it without a publish.
    effect(() => {
      const description = siteService.site().seo.description;
      if (!description) return;
      meta.updateTag({ name: 'description', content: description });
      meta.updateTag({ property: 'og:description', content: description });
    });
  }
}
