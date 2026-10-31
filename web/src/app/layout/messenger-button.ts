import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SiteService } from '../core/site.service';
import { Icon } from '../shared/icon';

/** Floating "Chat with us" button that opens the Facebook Messenger conversation. */
@Component({
  selector: 'app-messenger-button',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    a {
      position: fixed;
      right: 22px;
      bottom: 22px;
      z-index: 40;
      display: inline-flex;
      align-items: center;
      gap: 10px;
      padding: 12px 20px 12px 14px;
      border-radius: 999px;
      background: linear-gradient(135deg, #00b2ff, #006aff);
      color: #fff;
      font-weight: 600;
      font-size: 0.95rem;
      text-decoration: none;
      box-shadow: 0 16px 32px -12px rgba(0, 106, 255, 0.7);
      transition: transform 0.25s var(--ease);
      animation: pop-in 0.6s 1.2s var(--ease) both;
    }
    a:hover {
      transform: translateY(-3px) scale(1.02);
    }
    app-icon {
      width: 28px;
      height: 28px;
    }
    @keyframes pop-in {
      from {
        opacity: 0;
        transform: translateY(20px) scale(0.9);
      }
    }
    @media (max-width: 560px) {
      a {
        padding: 14px;
      }
      .label {
        display: none;
      }
    }
  `,
  template: `
    @if (siteService.site().messengerUrl) {
      <a [href]="siteService.site().messengerUrl" target="_blank" rel="noopener" aria-label="Chat with us on Facebook Messenger">
        <app-icon name="messenger" />
        <span class="label">Chat with us</span>
      </a>
    }
  `,
})
export class MessengerButton {
  protected readonly siteService = inject(SiteService);
}
