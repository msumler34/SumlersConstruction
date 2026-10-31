import { ViewportScroller } from '@angular/common';
import { afterRenderEffect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';

/**
 * The router scrolls to #fragment before data from the API has rendered the target section.
 * This scrolls again once `ready` turns true. Call it from a component constructor.
 */
export function scrollToFragmentWhenReady(ready: () => boolean): void {
  const scroller = inject(ViewportScroller);
  const fragment = toSignal(inject(ActivatedRoute).fragment);

  afterRenderEffect(() => {
    const target = fragment();
    if (target && ready()) {
      scroller.scrollToAnchor(target);
    }
  });
}
