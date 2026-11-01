import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Short photo banner at the top of the inner pages. */
@Component({
  selector: 'app-page-hero',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      position: relative;
      display: block;
      padding: 170px 0 90px;
      color: #fff;
      overflow: hidden;
    }
    img {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    :host::before {
      content: '';
      position: absolute;
      inset: 0;
      z-index: 1;
      background: linear-gradient(90deg, rgba(20, 26, 28, 0.9), rgba(20, 26, 28, 0.55));
    }
    .container {
      position: relative;
      z-index: 2;
    }
    .eyebrow {
      color: var(--amber-light);
    }
    h1 {
      font-size: clamp(2.3rem, 5vw, 3.8rem);
      max-width: 760px;
    }
    p {
      max-width: 620px;
      font-size: 1.12rem;
      color: rgba(255, 255, 255, 0.82);
      margin: 0;
    }
  `,
  // Content placed inside <app-page-hero> (breadcrumbs, buttons) shows under the text.
  template: `
    @if (image()) {
      <img [src]="image()" alt="" />
    }
    <div class="container">
      @if (eyebrow()) {
        <span class="eyebrow">{{ eyebrow() }}</span>
      }
      <h1>{{ title() }}</h1>
      @if (subtitle()) {
        <p>{{ subtitle() }}</p>
      }
      <ng-content />
    </div>
  `,
})
export class PageHero {
  readonly eyebrow = input('');
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly image = input('');
}
