import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ServiceOffering, SiteInfo } from '../../core/models';
import { EMPTY_SERVICE_PAGE } from '../../core/site.service';
import { Icon } from '../../shared/icon';
import { ImageField } from '../../shared/image-field';
import { SiteDraftEditor } from './site-draft-editor';

/** Edits the /services overview and each service's own page: text, photos, questions and Google details. */
@Component({
  selector: 'app-service-pages-editor',
  imports: [FormsModule, Icon, ImageField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './service-pages-editor.html',
  styleUrls: ['./admin-shared.scss', './site-editor.scss', './service-pages-editor.scss'],
})
export class ServicePagesEditor extends SiteDraftEditor {
  protected readonly serviceIcons = ['deck', 'home', 'kitchen', 'bath', 'addition', 'porch', 'tools', 'hammer', 'ruler', 'shield', 'handshake'];
  protected readonly savedMessage = 'Saved. The service pages now show the new content.';

  /** Each service's address when it was loaded, so a changed address can keep the old one as a redirect. */
  private originalKeys = new WeakMap<ServiceOffering, string>();

  protected newService(): ServiceOffering {
    return {
      key: '',
      title: '',
      summary: '',
      icon: 'hammer',
      imageUrl: '',
      highlights: [],
      previousKeys: [],
      page: structuredClone(EMPTY_SERVICE_PAGE),
    };
  }

  /** The address the server will give the page, shown while the owner types. */
  protected address(service: ServiceOffering): string {
    return '/' + slug(service.key || service.title || 'new-service');
  }

  protected googleTitle(service: ServiceOffering, site: SiteInfo): string {
    return service.page.seoTitle || `${service.title} | ${site.businessName}`;
  }

  protected removePreviousKey(service: ServiceOffering, index: number): void {
    service.previousKeys.splice(index, 1);
    this.changed();
  }

  protected override afterLoad(): void {
    this.originalKeys = new WeakMap();
    for (const service of this.draft()?.services ?? []) {
      if (service.key) this.originalKeys.set(service, service.key);
    }
  }

  protected override beforeSave(site: SiteInfo): SiteInfo {
    const drafts = this.draft()?.services ?? [];
    return {
      ...site,
      services: site.services.map((service, i) => {
        const original = this.originalKeys.get(drafts[i]);
        const moved = original && original !== slug(service.key || service.title);
        return moved && !service.previousKeys.includes(original)
          ? { ...service, previousKeys: [...service.previousKeys, original] }
          : service;
      }),
    };
  }
}

/** Matches the server's page addresses: lower case, words joined with dashes. */
function slug(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}
