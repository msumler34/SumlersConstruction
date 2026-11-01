import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Icon } from '../../shared/icon';
import { ImageField } from '../../shared/image-field';
import { SiteDraftEditor } from './site-draft-editor';

/** Edits the business details and home page in /api/site: contact details, social links, photos, gallery and reviews. */
@Component({
  selector: 'app-site-editor',
  imports: [FormsModule, Icon, ImageField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './site-editor.html',
  styleUrls: ['./admin-shared.scss', './site-editor.scss'],
})
export class SiteEditor extends SiteDraftEditor {
  protected readonly pointIcons = ['check', 'shield', 'ruler', 'handshake', 'clock', 'star', 'hammer', 'tools', 'home', 'chat'];
  protected readonly savedMessage = 'Saved. The whole site now shows the new details.';
}
