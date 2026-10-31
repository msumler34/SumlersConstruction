import { HttpErrorResponse } from '@angular/common/http';

export type AdminMessage = { kind: 'ok' | 'error'; text: string } | null;

export function describeError(err: HttpErrorResponse): string {
  if (err.status === 401) return 'Your admin session expired. Please sign in again.';
  const errors = err.error?.errors as Record<string, string[]> | undefined;
  if (errors) return Object.values(errors).flat().join(' ');
  return err.error?.error ?? 'Something went wrong. Please try again.';
}
