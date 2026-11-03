import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Floating button that opens the contact form, which emails the request. */
@Component({
  selector: 'app-estimate-button',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    a {
      position: fixed;
      right: 22px;
      bottom: 90px;
      z-index: 39;
      display: inline-flex;
      align-items: center;
      gap: 10px;
      padding: 12px 18px;
      border-radius: 999px;
      background: linear-gradient(135deg, #c9803a, #a86a2e);
      color: #fff;
      font-weight: 600;
      font-size: 0.92rem;
      text-decoration: none;
      box-shadow: 0 14px 28px -12px rgba(201, 128, 58, 0.7);
      transition: transform 0.25s ease;
      animation: pop-in 0.6s 1.4s ease both;
    }
    a:hover {
      transform: translateY(-3px) scale(1.02);
    }
    @keyframes pop-in {
      from {
        opacity: 0;
        transform: translateY(16px) scale(0.92);
      }
    }
    @media (max-width: 560px) {
      a {
        bottom: 86px;
        padding: 12px 14px;
        font-size: 0.85rem;
      }
    }
  `,
  template: `
    <a routerLink="/contact" aria-label="Email us for a free estimate">
      Email us
    </a>
  `,
})
export class EstimateButton {}