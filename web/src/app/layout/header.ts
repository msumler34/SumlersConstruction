import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map } from 'rxjs';
import { SiteService } from '../core/site.service';
import { Icon } from '../shared/icon';
import { Logo } from '../shared/logo';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, Icon, Logo],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(window:scroll)': 'onScroll()' },
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {
  protected readonly siteService = inject(SiteService);
  private readonly router = inject(Router);

  protected readonly menuOpen = signal(false);
  private readonly scrolled = signal(false);
  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  /** The header sits on top of the hero photo only at the top of the home page. */
  protected readonly solid = computed(
    () => this.scrolled() || this.menuOpen() || !/^\/(#.*)?$/.test(this.url()),
  );

  protected onScroll(): void {
    this.scrolled.set(window.scrollY > 40);
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }
}
