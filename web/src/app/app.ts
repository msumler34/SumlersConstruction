import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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
export class App {}
