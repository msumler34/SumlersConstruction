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
      background: #1877f2;
      color: white; display: flex; align-items: center; justify-content: center;
      box-shadow: 0 4px 12px rgba(24,119,242,.4);
      text-decoration: none;
    }
  `,
  template: `<a href="https://www.facebook.com/people/Sumlers-Construction-and-Remodeling/61596212907011/" target="_blank" rel="noopener" aria-label="Message us on Facebook"><app-icon name="facebook" /></a>`,
})
export class MessengerButton {}
