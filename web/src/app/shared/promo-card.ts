import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Promotion } from '../core/models';
import { Icon } from './icon';
import { SrcsetPipe } from './srcset';

/** One promoted topic. Internal links (starting with /) use the router, the rest open normally. */
@Component({
  selector: 'app-promo-card',
  imports: [RouterLink, Icon, SrcsetPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './promo-card.html',
  styleUrl: './promo-card.scss',
})
export class PromoCard {
  readonly promotion = input.required<Promotion>();

  protected readonly link = computed(() => splitLink(this.promotion().ctaUrl));
}

export function splitLink(url: string | null): { internal: boolean; path: string; query: Record<string, string> } {
  if (!url || !url.startsWith('/')) {
    return { internal: false, path: url ?? '', query: {} };
  }
  const [path, search = ''] = url.split('?');
  return { internal: true, path, query: Object.fromEntries(new URLSearchParams(search)) };
}
