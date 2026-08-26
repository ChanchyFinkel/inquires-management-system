import { HttpErrorResponse } from '@angular/common/http';

export type UiErrorKind = 'notFound' | 'validation' | 'server';

export interface UiError {
  kind: UiErrorKind;
  message: string;
}

const SAFE_MESSAGES: Record<UiErrorKind, string> = {
  notFound: 'הפריט המבוקש לא נמצא.',
  validation: 'הבקשה אינה תקינה. בדקו את הסינון ונסו שוב.',
  server: 'משהו השתבש בצד שלנו. נסו שוב בעוד רגע.',
};

// Never forwards err.error / problem+json.detail into the UI — only a fixed, safe string per kind.
// The raw response is still available to console.error in the interceptor for debugging.
export function toUiError(err: unknown): UiError {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 404) return { kind: 'notFound', message: SAFE_MESSAGES.notFound };
    if (err.status === 400) return { kind: 'validation', message: SAFE_MESSAGES.validation };
  }
  return { kind: 'server', message: SAFE_MESSAGES.server };
}
