import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Stroke icons, 24x24, drawn with currentColor. Brand icons (facebook, messenger) are filled. */
const PATHS: Record<string, string> = {
  phone:
    'M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.8.7 2.7a2 2 0 0 1-.5 2.1L8 9.8a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.8.6 2.7.7a2 2 0 0 1 1.7 2z',
  mail: 'M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zm18 2-10 7L2 6',
  pin: 'M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0zm-8 3a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
  clock: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zm0-14v4l3 2',
  arrow: 'M5 12h14m-6-6 6 6-6 6',
  check: 'M20 6 9 17l-5-5',
  star: 'm12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1z',
  shield: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10zm-3-10 2 2 4-4',
  hammer:
    'm15 12-8.4 8.4a2.1 2.1 0 0 1-3-3L12 9m5.6 6L22 10.6M20.9 11.7 12.3 3.1a1 1 0 0 0-1.4 0L9.4 4.6a1 1 0 0 0 0 1.4l8.6 8.6a1 1 0 0 0 1.4 0l1.5-1.5a1 1 0 0 0 0-1.4z',
  deck: 'M2 10h20M4 10v11m16-11v11M2 14h20M2 18h20M8 10 12 3l4 7',
  home: 'M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z',
  porch: 'M3 9 12 3l9 6M5 9v12m14-12v12M9 21v-6h6v6M2 21h20M5 13h14',
  kitchen: 'M4 3h16v18H4zm0 6h16M8 6h.01M12 6h.01M16 6h.01M8 13h8v5H8z',
  bath: 'M3 12h18v3a5 5 0 0 1-5 5H8a5 5 0 0 1-5-5zm3 0V5a2 2 0 0 1 4 0M7 20l-1 2m11-2 1 2',
  addition: 'M2 12 9 6l7 6M4 11v10h10V11m5-8v6m-3-3h6',
  tools:
    'M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9z',
  ruler: 'M21.3 8.7 8.7 21.3a1 1 0 0 1-1.4 0l-4.6-4.6a1 1 0 0 1 0-1.4L15.3 2.7a1 1 0 0 1 1.4 0l4.6 4.6a1 1 0 0 1 0 1.4zM7.5 10.5l2 2m1-5 2 2m1-5 2 2m-10 7 2 2',
  chat: 'M21 12a8.5 8.5 0 0 1-12.4 7.5L3 21l1.5-5.6A8.5 8.5 0 1 1 21 12z',
  handshake: 'm11 17 2 2a1 1 0 0 0 1.4 0l5.3-5.3a2 2 0 0 0 0-2.8L15 6.2a2 2 0 0 0-2.8 0L11 7.4 9.8 6.2a2 2 0 0 0-2.8 0L2.3 10.9m8.7 6.1-3-3m6 0-3-3',
  menu: 'M3 6h18M3 12h18M3 18h18',
  close: 'M18 6 6 18M6 6l12 12',
  quote: 'M7 7h4v4c0 3-1.5 5-4 6m6-10h4v4c0 3-1.5 5-4 6',
  upload: 'M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4m14-7-5-5-5 5m5-5v12',
  trash: 'M3 6h18m-2 0-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2',
  edit: 'M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z',
  plus: 'M12 5v14M5 12h14',
  lock: 'M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2zm2 0V7a5 5 0 0 1 10 0v4',
  eye: 'M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12zm10 3a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
};

const FILLED: Record<string, string> = {
  facebook:
    'M24 12a12 12 0 1 0-13.9 11.9v-8.4H7.1V12h3V9.4c0-3 1.8-4.7 4.5-4.7 1.3 0 2.7.2 2.7.2v3h-1.5c-1.5 0-2 .9-2 1.9V12h3.4l-.5 3.5h-2.9v8.4A12 12 0 0 0 24 12z',
  messenger:
    'M12 2C6.4 2 2 6.1 2 11.7c0 2.9 1.2 5.5 3.2 7.3.2.2.3.4.3.6l.1 1.8c0 .6.6.9 1.1.7l2-.9c.2-.1.4-.1.6 0 .9.3 1.8.4 2.7.4 5.6 0 10-4.1 10-9.7S17.6 2 12 2zm6 7.5-2.9 4.7c-.5.7-1.5.9-2.2.4l-2.3-1.7c-.2-.2-.5-.2-.7 0l-3.1 2.4c-.4.3-1-.2-.7-.6L9 10c.5-.7 1.5-.9 2.2-.4l2.3 1.7c.2.2.5.2.7 0l3.1-2.4c.4-.3 1 .2.7.6z',
};

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true' },
  styles: `
    :host {
      display: inline-block;
      width: 24px;
      height: 24px;
      flex-shrink: 0;
    }
    svg {
      width: 100%;
      height: 100%;
    }
  `,
  template: `
    @if (filled()) {
      <svg viewBox="0 0 24 24" fill="currentColor"><path [attr.d]="filled()" /></svg>
    } @else {
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
        <path [attr.d]="stroked()" />
      </svg>
    }
  `,
})
export class Icon {
  readonly name = input.required<string>();

  protected readonly filled = computed(() => FILLED[this.name()] ?? null);
  protected readonly stroked = computed(() => PATHS[this.name()] ?? PATHS['hammer']);
}
