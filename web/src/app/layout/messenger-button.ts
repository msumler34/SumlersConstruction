import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../shared/icon';

@Component({
  selector: 'app-messenger-button',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host { display: block; }
    a {
      position: fixed; bottom: 1.2rem; right: 1.2rem; z-index: 1000;
      width: 56px; height: 56px; border-radius: 50%;
      background: linear-gradient(135deg, #0084ff, #00c6ff);
      color: white; display: flex; align-items: center; justify-content: center;
      box-shadow: 0 4px 12px rgba(0,132,255,.4);
      text-decoration: none;
    }
  `,
  template: `<a href="https://m.me/sumlersconstruction" target="_blank" rel="noopener" aria-label="Message us on Facebook"><app-icon name="messenger" /></a>`,
})
export class MessengerButton {}
