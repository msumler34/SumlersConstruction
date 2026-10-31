import { Directive, ElementRef, OnDestroy, afterNextRender, inject, input } from '@angular/core';

/** Fades an element in the first time it scrolls into view. */
@Directive({
  selector: '[appReveal]',
  host: { class: 'reveal', '[style.transition-delay.ms]': 'appReveal() || 0' },
})
export class Reveal implements OnDestroy {
  readonly appReveal = input<number | ''>(0);

  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private observer?: IntersectionObserver;

  constructor() {
    afterNextRender(() => this.observe());
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private observe(): void {
    const target = this.element.nativeElement;
    if (!('IntersectionObserver' in window)) {
      target.classList.add('is-visible');
      return;
    }
    this.observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          target.classList.add('is-visible');
          this.observer?.disconnect();
        }
      },
      { threshold: 0.12, rootMargin: '0px 0px -40px 0px' },
    );
    this.observer.observe(target);
  }
}
