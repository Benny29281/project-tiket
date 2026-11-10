import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, finalize, Observable, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { AuthResponse, Role, User } from './models';

@Injectable({ providedIn: 'root' })
export class Auth {
  private http = inject(HttpClient);
  private router = inject(Router);
  private session = signal<AuthResponse | null>(this.read());
  private refreshCall?: Observable<AuthResponse>;
  user = computed<User | null>(() => this.session()?.user ?? null);
  staff = computed(() => ['Admin', 'Agent'].includes(this.user()?.role ?? ''));
  admin = computed(() => this.user()?.role === 'Admin');
  get token() { return this.session()?.accessToken; }
  private read(): AuthResponse | null {
    try { const value = JSON.parse(sessionStorage.getItem('helpdeskku.session') || 'null'); return value?.accessToken && value?.user ? value : null; } catch { return null; }
  }
  private save(value: AuthResponse) { this.session.set(value); sessionStorage.setItem('helpdeskku.session', JSON.stringify(value)); }
  login(body: unknown) { return this.http.post<AuthResponse>('/api/v1/auth/login', body).pipe(tap(value => this.save(value))); }
  register(body: unknown) { return this.http.post<AuthResponse>('/api/v1/auth/register', body).pipe(tap(value => this.save(value))); }
  refresh() {
    if (!this.session()?.refreshToken) return throwError(() => new Error('Sesi berakhir'));
    return this.refreshCall ??= this.http.post<AuthResponse>('/api/v1/auth/refresh', { refreshToken: this.session()!.refreshToken }).pipe(
      tap(value => this.save(value)), catchError(error => { this.clear(); return throwError(() => error); }),
      finalize(() => { this.refreshCall = undefined; }), shareReplay({ bufferSize: 1, refCount: false }));
  }
  logout() { return this.http.post('/api/v1/auth/logout', { refreshToken: this.session()?.refreshToken }).pipe(finalize(() => this.clear())); }
  clear() { this.session.set(null); sessionStorage.removeItem('helpdeskku.session'); void this.router.navigateByUrl('/login'); }
}
export const authGuard: CanActivateFn = () => inject(Auth).user() ? true : inject(Router).createUrlTree(['/login']);
export const roleGuard: CanActivateFn = route => (route.data['roles'] as Role[]).includes(inject(Auth).user()?.role as Role) ? true : inject(Router).createUrlTree(['/dashboard']);
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(Auth);
  if (!request.url.startsWith('/api/v1/')) return next(request);
  const isAuth = /\/auth\/(login|register|refresh)$/.test(request.url);
  const authorized = auth.token && !isAuth ? request.clone({ setHeaders: { Authorization: 'Bearer ' + auth.token } }) : request;
  return next(authorized).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status !== 401 || isAuth || !auth.token) return throwError(() => error);
    if (request.url.endsWith('/auth/logout')) { auth.clear(); return throwError(() => error); }
    return auth.refresh().pipe(switchMap(session => next(request.clone({ setHeaders: { Authorization: 'Bearer ' + session.accessToken } }))),
      catchError(refreshError => { if (refreshError.status === 401) auth.clear(); return throwError(() => refreshError); }));
  }));
};
