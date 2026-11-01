import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from './core/seo.service';
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
    // Each page's title and Google description come from the admin page, so the owner can change them without a publish.
    inject(SeoService).start();
  }
}
