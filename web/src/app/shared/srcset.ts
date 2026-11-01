import { Pipe, PipeTransform } from '@angular/core';

/** The widths the server makes from each upload (PhotoSizeEndpoints.Widths). */
const WIDTHS = [400, 680, 760, 960, 1280, 1920];
const UPLOAD = /^\/uploads\/([0-9a-f]{32})\.(jpe?g|png|webp)$/i;

/**
 * The srcset for a photo, so a phone downloads a phone-sized copy instead of the full photo.
 * Null for an address the site can't resize, which leaves the attribute off.
 */
export function srcset(url: string | null | undefined): string | null {
  if (!url) return null;
  const upload = UPLOAD.exec(url);
  if (upload) return WIDTHS.map((w) => `/uploads/${upload[1]}-w${w}.webp ${w}w`).join(', ');
  if (url.startsWith('https://images.unsplash.com/')) return WIDTHS.map((w) => `${unsplashAt(url, w)} ${w}w`).join(', ');
  return null;
}

/** Unsplash resizes on its own servers through the w= and q= settings in the address. */
function unsplashAt(url: string, width: number): string {
  const address = new URL(url);
  address.searchParams.set('w', String(width));
  address.searchParams.set('q', '70');
  address.searchParams.set('auto', 'format');
  return address.toString();
}

/** Usage: <img [src]="url" [attr.srcset]="url | srcset" sizes="100vw" /> */
@Pipe({ name: 'srcset' })
export class SrcsetPipe implements PipeTransform {
  transform(url: string | null | undefined): string | null {
    return srcset(url);
  }
}
