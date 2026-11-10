import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);
  get<T>(path: string, params: Record<string, string | number | boolean> = {}) { return this.http.get<T>('/api/v1/' + path, { params: new HttpParams({ fromObject: params }) }); }
  post<T>(path: string, body: unknown) { return this.http.post<T>('/api/v1/' + path, body); }
  put<T>(path: string, body: unknown) { return this.http.put<T>('/api/v1/' + path, body); }
  patch<T>(path: string, body: unknown) { return this.http.patch<T>('/api/v1/' + path, body); }
  delete(path: string) { return this.http.delete('/api/v1/' + path); }
}
export function errorMessage(error: HttpErrorResponse): string {
  if (error.status === 0 || error.status === 502 || error.status === 504) return 'Server belum dapat dihubungi. Pastikan API dan SQL Server sudah berjalan.';
  if (error.status === 403) return 'Anda tidak memiliki akses untuk tindakan ini.';
  if (error.status === 404) return 'Data tidak ditemukan atau tidak dapat diakses.';
  if (error.status === 429) return 'Terlalu banyak percobaan. Tunggu satu menit, lalu coba lagi.';
  if (error.error?.errors) return Object.values(error.error.errors as Record<string, string[]>).flat().join(' ');
  return error.error?.detail || error.error?.title || 'Terjadi kesalahan. Silakan coba kembali.';
}
