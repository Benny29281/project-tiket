import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Auth } from '../core/auth';
import { errorMessage } from '../core/api';
import { Icon } from '../shared/icon';

@Component({ imports: [ReactiveFormsModule, RouterLink, Icon], template: `
  <div class="auth-page"><section class="auth-story"><a class="brand" routerLink="/"><span class="brand-icon"><hd-icon name="headset" /></span>HelpDesk<span>Ku</span></a>
    <div><p class="eyebrow">YOUR TEAM'S SUPPORT SPACE</p><h1>Bantuan yang tepat.<br>Kerja lebih lancar.</h1><p>Satukan setiap permintaan, hubungkan tim,<br>dan selesaikan kendala dengan lebih terarah.</p><div class="auth-graphic"><span><hd-icon name="ticket" /> Satu permintaan</span><i></i><span><hd-icon name="users" /> Tim yang siap</span><i></i><span><hd-icon name="check" /> Solusi yang jelas</span></div></div>
    <small>IT Helpdesk · Dibangun untuk kerja bersama</small></section>
  <section class="auth-panel"><div class="auth-form"><span class="mini-label">MULAI DARI SINI</span><h2>{{ register ? 'Buat akun Anda' : 'Selamat datang kembali' }}</h2><p class="muted">{{ register ? 'Laporkan kendala dan pantau penyelesaiannya.' : 'Masuk untuk melanjutkan ke ruang kerja Anda.' }}</p>
    @if (error()) { <div class="error" role="alert">{{ error() }}</div> }
    <form [formGroup]="form" (ngSubmit)="submit()">
      @if (register) { <label>Nama lengkap<input formControlName="fullName" autocomplete="name" placeholder="Nama lengkap Anda" maxlength="100"></label> }
      <label>Email<input type="email" formControlName="email" autocomplete="username" placeholder="nama@perusahaan.com"></label>
      <label>Password<input type="password" formControlName="password" [autocomplete]="register ? 'new-password' : 'current-password'" placeholder="Masukkan password" maxlength="128"></label>
      @if (register) { <p class="field-help">Minimal 10 karakter, huruf besar, huruf kecil, angka, dan simbol.</p> }
      @if (form.touched && form.invalid) { <p class="error-text">Lengkapi seluruh kolom dengan email dan password yang valid.</p> }
      <button class="button primary full" [disabled]="busy()">{{ busy() ? 'Memproses…' : register ? 'Buat akun' : 'Masuk ke workspace' }}<hd-icon name="arrow" /></button>
    </form><p class="auth-switch">{{ register ? 'Sudah punya akun?' : 'Belum punya akun?' }} <a [routerLink]="register ? '/login' : '/register'">{{ register ? 'Masuk' : 'Daftar sekarang' }}</a></p>
    <div class="auth-security"><hd-icon name="lock" /> Akses aman untuk tim Anda</div>
  </div></section></div>` })
export class LoginComponent {
  private fb = inject(FormBuilder); private auth = inject(Auth); private router = inject(Router);
  register = inject(ActivatedRoute).snapshot.routeConfig?.path === 'register';
  error = signal(''); busy = signal(false);
  form = this.fb.nonNullable.group({ fullName: ['', this.register ? [Validators.required, Validators.minLength(2)] : []], email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required, ...(this.register ? [Validators.minLength(10)] : [])]] });
  submit() {
    this.form.markAllAsTouched(); if (this.form.invalid || this.busy()) return;
    this.busy.set(true); this.error.set('');
    (this.register ? this.auth.register(this.form.getRawValue()) : this.auth.login(this.form.getRawValue())).subscribe({
      next: () => { void this.router.navigateByUrl('/dashboard'); }, error: error => { this.error.set(errorMessage(error)); this.busy.set(false); }
    });
  }
}
