import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toUiError } from '../models/ui-error.model';

export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      console.error(`[API] ${req.method} ${req.url} failed`, err.status, err.error);
      return throwError(() => toUiError(err));
    }),
  );
