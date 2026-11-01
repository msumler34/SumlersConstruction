import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 12px;
      color: inherit;
    }
    img {
      width: 48px;
      height: 48px;
      object-fit: contain;
    }
    .word {
      display: grid;
      line-height: 1;
    }
    .name {
      font-family: var(--font-display);
      font-size: 1.45rem;
      font-weight: 700;
      letter-spacing: 0.01em;
    }
    .sub {
      font-size: 0.62rem;
      font-weight: 600;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      opacity: 0.75;
      margin-top: 5px;
    }
  `,
  template: `
    <img src="/sumler-logo.png" alt="Sumler's Construction and Remodeling" />
    <span class="word">
      <span class="name">Sumler's</span>
      <span class="sub">Construction and Remodeling</span>
    </span>
  `,
})
export class Logo {}