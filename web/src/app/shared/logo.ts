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
    svg {
      width: 42px;
      height: 42px;
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
      letter-spacing: 0.34em;
      text-transform: uppercase;
      opacity: 0.75;
      margin-top: 5px;
    }
  `,
  template: `
    <svg viewBox="0 0 64 64" aria-hidden="true">
      <rect width="64" height="64" rx="14" fill="#c9803a" />
      <path d="M12 30 32 14l20 16" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
      <path d="M18 38h28M18 45h28M18 52h28" stroke="#1b2326" stroke-width="4" stroke-linecap="round" />
    </svg>
    <span class="word">
  <span class="name">Sumler's</span>
  <span class="sub">Construction and Remodeling</span>
</span>
  `,
})
export class Logo {}
