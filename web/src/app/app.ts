import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from './core/seo.service';
import { Footer } from './layout/footer';
import { Header } from './layout/header';
import { MessengerButton } from './layout/messenger-button';
import { EstimateButton } from './layout/estimate-button';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer, MessengerButton, EstimateButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-header />
    <main>
      <router-outlet />
    </main>
    <app-footer />
    <app-messenger-button />
    <app-estimate-button />
  `,
})
export class App {
  constructor() {
    inject(SeoService).start();
  }
}