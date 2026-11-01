import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Icon } from '../shared/icon';

/** Shown for addresses the site does not have. The server also answers these with a 404 status. */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: block;
      padding: 190px 0 120px;
      text-align: center;
    }
    h1 {
      font-size: clamp(2.2rem, 5vw, 3.4rem);
    }
    p {
      color: var(--ink-soft);
      max-width: 520px;
      margin: 0 auto 32px;
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 12px;
    }
  `,
  template: `
    <div class="container">
      <span class="eyebrow">Page not found</span>
      <h1>We couldn't find that page</h1>
      <p>The link may be old, or the page may have moved. Here are the best places to start.</p>
      <div class="actions">
        <a routerLink="/" class="btn btn--primary">Home page <app-icon name="arrow" /></a>
        <a routerLink="/services" class="btn btn--outline">Our services</a>
        <a routerLink="/contact" class="btn btn--outline">Free estimate</a>
      </div>
    </div>
  `,
})
export class NotFound {}
